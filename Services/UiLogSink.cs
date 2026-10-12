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
	/// <summary>有新日志产生（可能在后台线程）</summary>
	public event Action<UiLogEntry>? EntryWritten;

	/// <summary>直接写入一条界面日志（不经过 ILogger）</summary>
	public void Write(UiLogLevel level, string message)
		=> EntryWritten?.Invoke(new UiLogEntry(DateTime.Now, level, message));

	public ILogger CreateLogger(string categoryName) => new SinkLogger(this);

	public void Dispose()
	{
	}

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
