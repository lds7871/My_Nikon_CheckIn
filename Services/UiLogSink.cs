using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NikonCheckIn.Gui.Services;

/// <summary>界面日志级别</summary>
public enum UiLogLevel
{
	Info,
	Success,
	Warning,
	Error
}

/// <summary>一条界面日志</summary>
public sealed record UiLogEntry(DateTime Time, UiLogLevel Level, string Message);

/// <summary>
/// 把 <see cref="ILogger"/> 的输出转发到界面日志面板。
/// 现有的 LoginService / CheckInService / CommentService 保持原样，日志直接出现在右侧。
/// </summary>
public sealed class UiLogSink : ILoggerProvider
{
	/// <summary>手机号：138****5678（前后仅排除数字，避免紧贴中文时漏打码）</summary>
	private static readonly Regex PhonePattern = new(@"(?<!\d)1[3-9]\d{9}(?!\d)", RegexOptions.Compiled);

	/// <summary>需要打码的敏感内容（如密码），命中后整体替换</summary>
	private readonly List<string> _secrets = new();

	/// <summary>有新日志产生（可能在后台线程）</summary>
	public event Action<UiLogEntry>? EntryWritten;

	/// <summary>注册需要打码的敏感内容；太短的会误伤日志，直接忽略</summary>
	public void RegisterSecret(string? secret)
	{
		if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 6 && !_secrets.Contains(secret))
		{
			_secrets.Add(secret);
		}
	}

	/// <summary>直接写入一条界面日志（不经过 ILogger）</summary>
	public void Write(UiLogLevel level, string message)
		=> EntryWritten?.Invoke(new UiLogEntry(DateTime.Now, level, Mask(message)));

	public ILogger CreateLogger(string categoryName) => new SinkLogger(this);

	public void Dispose()
	{
	}

	/// <summary>日志里的手机号与已注册的敏感内容一律打码</summary>
	private string Mask(string text)
	{
		var masked = PhonePattern.Replace(text, match => MaskPhone(match.Value));

		foreach (var secret in _secrets)
		{
			masked = masked.Replace(secret, "******", StringComparison.Ordinal);
		}

		return masked;
	}

	private static string MaskPhone(string phone)
		=> phone.Length == 11 ? $"{phone[..3]}****{phone[7..]}" : "****";

	private void Emit(LogLevel level, string message)
		=> Write(level switch
		{
			LogLevel.Error or LogLevel.Critical => UiLogLevel.Error,
			LogLevel.Warning => UiLogLevel.Warning,
			_ => UiLogLevel.Info
		}, message);

	private sealed class SinkLogger(UiLogSink owner) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var message = formatter(state, exception);
			if (exception is not null)
			{
				// 把异常类型与内部异常一起写出来，方便直接复制报错内容排查
				message = $"{message} | {Describe(exception)}";
			}

			owner.Emit(logLevel, message);
		}

		private static string Describe(Exception exception)
		{
			var builder = new System.Text.StringBuilder();
			builder.Append(exception.GetType().Name).Append(": ").Append(exception.Message);

			var inner = exception.InnerException;
			var depth = 0;
			while (inner is not null && depth < 5)
			{
				builder.Append(" <- ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
				inner = inner.InnerException;
				depth++;
			}

			return builder.ToString();
		}
	}
}
