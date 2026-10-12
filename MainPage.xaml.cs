using Microsoft.Maui.ApplicationModel.DataTransfer;
using NikonCheckIn.Gui.Services;
using NikonCheckIn.Models;

namespace NikonCheckIn.Gui;

public partial class MainPage : ContentPage
{
	/// <summary>日志面板最多保留的行数，避免长时间运行占用过多内存</summary>
	private const int MaxLogLines = 800;

	/// <summary>Edge 浏览器下载页</summary>
	private const string EdgeDownloadUrl = "https://www.microsoft.com/edge/download";

	private readonly UiLogSink _logSink = new();
	private readonly CheckInRunner _runner;

	/// <summary>配置（账号、密码由输入框覆盖，其余项来自 账号密码配置.txt）</summary>
	private AppConfig _config = new();

	private bool _running;
	private bool _browserChecked;

	/// <summary>界面初始化完成前，开关的赋值不写日志</summary>
	private bool _uiReady;

	public MainPage()
	{
		InitializeComponent();

		LogLabel.FormattedText = new FormattedString();

		_runner = new CheckInRunner(_logSink);
		_logSink.EntryWritten += OnLogEntryWritten;

		LoadConfig();

		// 开关初值跟随配置（账号密码配置.txt 的“无头模式”）
		HeadlessSwitch.IsToggled = _config.HeadlessMode;

		WriteStartupLog();
		_uiReady = true;
	}

	/// <summary>无头模式开关</summary>
	private void OnHeadlessToggled(object? sender, ToggledEventArgs e)
	{
		_config.HeadlessMode = e.Value;

		if (_uiReady)
		{
			_logSink.Write(UiLogLevel.Info,
				$"无头模式: {(e.Value ? "启用（不显示浏览器窗口）" : "禁用（显示浏览器窗口）")}");
		}
	}

	/// <summary>打开应用时自动检查浏览器适配情况</summary>
	protected override void OnAppearing()
	{
		base.OnAppearing();

		CompactHeadlessSwitch();

		if (_browserChecked)
		{
			return;
		}

		_browserChecked = true;
		CheckBrowser();
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		EnableLogSelection();
		CompactHeadlessSwitch();
	}

	/// <summary>让日志文本可以用鼠标选中（Windows 下 WinUI TextBlock 的文本选择）</summary>
	private void EnableLogSelection()
	{
#if WINDOWS
		if (LogLabel.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
		{
			textBlock.IsTextSelectionEnabled = true;
		}
#endif
	}

	/// <summary>
	/// Windows 的 ToggleSwitch 自带 On/Off 内容区且最小宽度 154，
	/// 会把旁边的文字推得很远；这里去掉内容区并解除最小宽度，让文字紧贴开关。
	/// </summary>
	private void CompactHeadlessSwitch()
	{
#if WINDOWS
		if (HeadlessSwitch.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ToggleSwitch toggle)
		{
			toggle.OnContent = null;
			toggle.OffContent = null;
			toggle.MinWidth = 0;
			toggle.Padding = new Microsoft.UI.Xaml.Thickness(0);
			toggle.Margin = new Microsoft.UI.Xaml.Thickness(0);
		}
#endif
	}

	// ============================ 日志 ============================

	private void OnLogEntryWritten(UiLogEntry entry)
	{
		// 日志可能来自后台线程（Playwright 流程），统一切回 UI 线程
		MainThread.BeginInvokeOnMainThread(() => AppendLog(entry));
	}

	private void AppendLog(UiLogEntry entry)
	{
		var spans = LogLabel.FormattedText.Spans;

		spans.Add(new Microsoft.Maui.Controls.Span
		{
			Text = $"{entry.Time:HH:mm:ss}  {entry.Message}{Environment.NewLine}",
			TextColor = ColorFor(entry.Level)
		});

		while (spans.Count > MaxLogLines)
		{
			spans.RemoveAt(0);
		}

		// FormattedText 更新后重新确保日志可选中
		EnableLogSelection();

		_ = LogScrollView.ScrollToAsync(LogLabel, ScrollToPosition.End, false);
	}

	/// <summary>把整份日志复制到剪贴板，便于把报错内容发出去</summary>
	private async void OnCopyLogClicked(object? sender, EventArgs e)
	{
		var text = string.Concat(LogLabel.FormattedText.Spans.Select(s => s.Text));

		if (string.IsNullOrWhiteSpace(text))
		{
			_logSink.Write(UiLogLevel.Warning, "当前没有日志可复制。");
			return;
		}

		try
		{
			await Clipboard.Default.SetTextAsync(text);
			var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length;
			_logSink.Write(UiLogLevel.Success, $"已复制 {lines} 行日志到剪贴板。");
		}
		catch (Exception ex)
		{
			_logSink.Write(UiLogLevel.Error, $"复制日志失败: {ex.Message}");
		}
	}

	private static Color ColorFor(UiLogLevel level) => level switch
	{
		UiLogLevel.Success => Color.FromArgb("#1E9E5A"),
		UiLogLevel.Warning => Color.FromArgb("#C77700"),
		UiLogLevel.Error => Color.FromArgb("#D93025"),
		_ => Color.FromArgb("#2B3240")
	};

	private void WriteStartupLog()
	{
		_logSink.Write(UiLogLevel.Info, "尼康自动签到已就绪，请输入账号密码后点击“开始签到”。");
		if (string.IsNullOrWhiteSpace(AccountEntry.Text) && string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{
			_logSink.Write(UiLogLevel.Info, "未读取到账号密码配置，请手动输入。");
		}
		else
		{
			_logSink.Write(UiLogLevel.Info, "已从 账号密码配置.txt 预填账号密码。");
		}

		_logSink.Write(UiLogLevel.Info,
			$"无头模式: {(_config.HeadlessMode ? "启用（不显示浏览器窗口）" : "禁用（显示浏览器窗口）")}，基础等待时间: {_config.BaseWaitTimeMs} ms");
	}

	// ============================ 浏览器适配检查 ============================

	private void CheckBrowser()
	{
		var detected = BrowserProbe.Detect();

		if (detected.Count > 0)
		{
			var first = detected[0];
			var version = string.IsNullOrWhiteSpace(first.Version) ? string.Empty : $" {first.Version}";

			BrowserStatusLabel.Text = $"已检测到 {first.Name}{version}";
			BrowserStatusLabel.TextColor = Color.FromArgb("#1E9E5A"); // 绿色
			DownloadEdgeLabel.IsVisible = false;

			_logSink.Write(UiLogLevel.Success, $"浏览器适配检查通过：{first.Name}{version}");
			foreach (var extra in detected.Skip(1))
			{
				var extraVersion = string.IsNullOrWhiteSpace(extra.Version) ? string.Empty : $" {extra.Version}";
				_logSink.Write(UiLogLevel.Info, $"另检测到：{extra.Name}{extraVersion}");
			}

			return;
		}

		BrowserStatusLabel.Text = "未检测到适配的浏览器";
		BrowserStatusLabel.TextColor = Color.FromArgb("#D93025"); // 红色
		DownloadEdgeLabel.IsVisible = true;

		_logSink.Write(UiLogLevel.Error, "浏览器适配检查未通过：未检测到 Chrome / Edge，请下载并安装 Edge 浏览器后重新打开本程序。");
		if (!BrowserProbe.HasBundledChromium())
		{
			_logSink.Write(UiLogLevel.Warning, "Playwright 内置 Chromium 也未安装，签到将无法启动浏览器。");
		}
	}

	private async void OnDownloadEdgeTapped(object? sender, TappedEventArgs e)
	{
		try
		{
			await Launcher.OpenAsync(new Uri(EdgeDownloadUrl));
			_logSink.Write(UiLogLevel.Info, $"已打开浏览器下载页面：{EdgeDownloadUrl}");
		}
		catch (Exception ex)
		{
			_logSink.Write(UiLogLevel.Warning, $"无法打开下载页面: {ex.Message}");
		}
	}

	// ============================ 配置 ============================

	private void LoadConfig()
	{
		// 与原有控制台程序保持一致：先找程序目录，再找当前工作目录
		var configFile = Path.Combine(AppContext.BaseDirectory, "账号密码配置.txt");
		if (!File.Exists(configFile))
		{
			configFile = Path.Combine(Directory.GetCurrentDirectory(), "账号密码配置.txt");
		}

		if (!File.Exists(configFile))
		{
			return;
		}

		try
		{
			_config = AppConfig.LoadFromFile(configFile);
			AccountEntry.Text = _config.PhoneNumber;
			PasswordEntry.Text = _config.Password;
		}
		catch (Exception ex)
		{
			_logSink.Write(UiLogLevel.Warning, $"读取 {configFile} 失败: {ex.Message}");
		}
	}

	// ============================ 开始签到 ============================

	private async void OnStartClicked(object? sender, EventArgs e)
	{
		if (_running)
		{
			return;
		}

		var account = AccountEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;

		if (account.Length == 0 || password.Length == 0)
		{
			_logSink.Write(UiLogLevel.Warning, "请先输入账号和密码。");
			return;
		}

		_config.PhoneNumber = account;
		_config.Password = password;

		_running = true;
		StartButton.IsEnabled = false;
		StartButton.Text = "签到中...";

		try
		{
			await Task.Run(() => _runner.RunAsync(_config));
		}
		catch (Exception ex)
		{
			_logSink.Write(UiLogLevel.Error, $"签到流程异常: {ex.Message}");
		}
		finally
		{
			_running = false;
			StartButton.IsEnabled = true;
			StartButton.Text = "开始签到";
		}
	}
}
