using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NikonCheckIn.Models;
using NikonCheckIn.Services;

namespace NikonCheckIn.Gui.Services;

/// <summary>
/// 签到流程编排：与原有控制台 Program.cs 一致（登录 → 签到/点赞 → 评论），
/// 但把日志转发到界面，并在开始前按需检查浏览器适配情况。
/// </summary>
public sealed class CheckInRunner
{
	private const string LoginUrl = "https://my.nikon.com.cn/user/level/task";

	private readonly UiLogSink _sink;

	public CheckInRunner(UiLogSink sink) => _sink = sink;

	public async Task RunAsync(AppConfig config)
	{
		using var loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.SetMinimumLevel(LogLevel.Information);
			builder.AddProvider(_sink);
		});

		var logger = loggerFactory.CreateLogger("签到");

		logger.LogInformation("开始执行签到流程...");

		var channelOrder = ResolveChannels(logger);

		logger.LogInformation("无头模式: {Mode}", config.HeadlessMode ? "启用" : "禁用");

		using var playwright = await Playwright.CreateAsync();

		var browserArgs = new List<string>();
		if (!config.HeadlessMode)
		{
			browserArgs.Add("--start-maximized");
		}

		var browser = await LaunchAsync(playwright, logger, config, browserArgs, channelOrder);
		if (browser is null)
		{
			_sink.Write(UiLogLevel.Error, "签到结束：未找到任何可用的浏览器。");
			return;
		}

		var context = await browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = ViewportSize.NoViewport, // 禁用视口限制，相当于最大化
			Locale = "zh-CN"
		});

		var page = await context.NewPageAsync();

		try
		{
			logger.LogInformation("打开登录页面...");
			await page.GotoAsync(LoginUrl);

			logger.LogInformation("执行登录流程...");
			var loginService = new LoginService(loggerFactory.CreateLogger<LoginService>(), config);
			await loginService.DoLoginAsync(page);

			logger.LogInformation("登录完成，执行 CheckIn 流程...");
			var checkInService = new CheckInService(loggerFactory.CreateLogger<CheckInService>(), config);
			await checkInService.DoCheckInAsync(page);

			logger.LogInformation("CheckIn 完成，执行评论流程...");
			var commentService = new CommentService(loggerFactory.CreateLogger<CommentService>(), config);
			await commentService.DoCommentAsync(page);

			_sink.Write(UiLogLevel.Success, "所有流程执行完毕 ✓");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "自动化过程中发生异常");
			_sink.Write(UiLogLevel.Error, "签到结束：流程中断。");
		}
		finally
		{
			await browser.CloseAsync();
			logger.LogInformation("浏览器已关闭。");
		}
	}

	/// <summary>记录本机浏览器情况，并返回启动通道的尝试顺序</summary>
	private static string[] ResolveChannels(ILogger logger)
	{
		logger.LogInformation("正在检查本机可用浏览器...");
		var detected = BrowserProbe.Detect();

		if (detected.Count == 0)
		{
			logger.LogWarning("未检测到本机 Chrome / Edge。");
			if (!BrowserProbe.HasBundledChromium())
			{
				logger.LogWarning("Playwright 内置 Chromium 也未安装，请先执行 playwright install chromium，或安装 Chrome / Edge。");
			}
			else
			{
				logger.LogInformation("将使用 Playwright 内置 Chromium。");
			}
		}
		else
		{
			foreach (var item in detected)
			{
				var version = string.IsNullOrWhiteSpace(item.Version) ? string.Empty : $" {item.Version}";
				logger.LogInformation("检测到本地浏览器: {Name}{Version}", item.Name, version);
			}
		}

		return BrowserProbe.ChannelOrder(detected);
	}

	/// <summary>依次尝试本地浏览器通道，最后回退到内置 Chromium</summary>
	private static async Task<Microsoft.Playwright.IBrowser?> LaunchAsync(IPlaywright playwright, ILogger logger, AppConfig config,
		List<string> browserArgs, string[] channelOrder)
	{
		foreach (var channel in channelOrder)
		{
			try
			{
				var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
				{
					Headless = config.HeadlessMode,
					Channel = channel,
					Args = browserArgs.ToArray()
				});

				logger.LogInformation("已启动浏览器: {Channel}", channel);
				return browser;
			}
			catch (PlaywrightException ex)
			{
				logger.LogDebug("未找到 {Channel}: {Message}", channel, ex.Message);
			}
		}

		try
		{
			var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
			{
				Headless = config.HeadlessMode,
				Args = browserArgs.ToArray()
			});

			logger.LogInformation("使用内置 Chromium 浏览器");
			return browser;
		}
		catch (PlaywrightException ex)
		{
			logger.LogError("未找到任何可用的浏览器。请安装 Chrome 或 Edge。错误: {Message}", ex.Message);
			return null;
		}
	}
}
