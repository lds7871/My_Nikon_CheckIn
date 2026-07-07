using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using NikonCheckIn.Models;
using NikonCheckIn.Services;

// 配置控制台编码
Console.OutputEncoding = System.Text.Encoding.UTF8;

// 创建日志工厂
using var loggerFactory = LoggerFactory.Create(builder =>
{
  builder.AddSimpleConsole(options =>
  {
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
  });
  builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("Program");
logger.LogInformation("开始执行组合流程: 一个浏览器会话内，先登录，再执行 CheckIn...");

// 读取配置
var configDir = AppDomain.CurrentDomain.BaseDirectory;
var configFile = Path.Combine(configDir, "账号密码配置.txt");

// 也尝试从当前工作目录读取
if (!File.Exists(configFile))
{
  configFile = Path.Combine(Directory.GetCurrentDirectory(), "账号密码配置.txt");
}

var config = AppConfig.LoadFromFile(configFile);
logger.LogInformation("无头模式: {Mode}", config.HeadlessMode ? "启用" : "禁用");

// 启动 Playwright
using var playwright = await Playwright.CreateAsync();

// 构建浏览器启动参数
var browserArgs = new List<string>();
if (!config.HeadlessMode)
{
  browserArgs.Add("--start-maximized");
}

// 自动检测本地可用的 Chromium 系浏览器（Chrome > Edge > ...）
string[] browserChannels = { "chrome", "msedge", "chrome-beta", "msedge-beta", "chrome-dev", "msedge-dev" };
string? usedChannel = null;

IBrowser browser = null!;
foreach (var channel in browserChannels)
{
  try
  {
    var options = new BrowserTypeLaunchOptions
    {
      Headless = config.HeadlessMode,
      Channel = channel,
      Args = browserArgs.ToArray()
    };
    browser = await playwright.Chromium.LaunchAsync(options);
    usedChannel = channel;
    logger.LogInformation("检测到本地浏览器: {Channel}", channel);
    break;
  }
  catch (PlaywrightException)
  {
    logger.LogDebug("未找到 {Channel}，尝试下一个...", channel);
  }
}

// 如果所有 Channel 都失败，尝试无 Channel 启动（需要 .local-browsers 中有 Chromium）
if (browser == null)
{
  try
  {
    var options = new BrowserTypeLaunchOptions
    {
      Headless = config.HeadlessMode,
      Args = browserArgs.ToArray()
    };
    browser = await playwright.Chromium.LaunchAsync(options);
    usedChannel = "chromium (bundled)";
    logger.LogInformation("使用内置 Chromium 浏览器");
  }
  catch (PlaywrightException ex)
  {
    logger.LogError("未找到任何可用的浏览器。请安装 Chrome 或 Edge。");
    logger.LogError("错误: {Message}", ex.Message);
    Environment.Exit(1);
  }
}
var context = await browser.NewContextAsync(new()
{
  ViewportSize = ViewportSize.NoViewport, // 禁用视口限制，相当于最大化
  Locale = "zh-CN"
});

var page = await context.NewPageAsync();

try
{
  // ============ 打开登录页面
  logger.LogInformation("Edge 浏览器已启动，打开登录页面...");
  await page.GotoAsync("https://my.nikon.com.cn/user/level/task");

  // ============ 执行登录流程
  logger.LogInformation("执行登录流程...");
  var loginService = new LoginService(loggerFactory.CreateLogger<LoginService>(), config);
  await loginService.DoLoginAsync(page);

  // ============ CheckIn 流程
  logger.LogInformation("登录完成，执行 CheckIn 流程...");
  var checkInService = new CheckInService(loggerFactory.CreateLogger<CheckInService>(), config);
  await checkInService.DoCheckInAsync(page);

  // ============ 帖子分享流程 -- 暂时关闭
  // logger.LogInformation("CheckIn 完成，执行帖子分享流程...");
  // var shareService = new ShareService(loggerFactory.CreateLogger<ShareService>(), config);
  // await shareService.DoPostShareAsync(page);

  // ============ 评论流程
  logger.LogInformation("CheckIn 完成，执行评论流程...");
  var commentService = new CommentService(loggerFactory.CreateLogger<CommentService>(), config);
  await commentService.DoCommentAsync(page);

  logger.LogInformation("所有流程执行完毕，程序即将关闭...");
}
catch (Exception ex)
{
  logger.LogError(ex, "自动化过程中发生异常");
  Environment.Exit(1);
}
finally
{
  await browser.CloseAsync();
}
