using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace NikonCheckIn.Services;

/// <summary>
/// 登录服务 - 处理尼康网站登录流程
/// 对应Java版本: OpenAndLogIn.java
/// </summary>
public class LoginService
{
  private readonly ILogger<LoginService> _logger;
  private readonly Models.AppConfig _config;
  private const string TargetUrl = "https://my.nikon.com.cn/user/level/task";
  private const int PageTimeoutMs = 30000;

  public LoginService(ILogger<LoginService> logger, Models.AppConfig config)
  {
    _logger = logger;
    _config = config;
  }

  /// <summary>
  /// 执行登录流程
  /// </summary>
  public async Task DoLoginAsync(IPage page)
  {
    await WaitForPageLoadedAsync(page);
    var title = await page.TitleAsync();
    _logger.LogInformation("页面加载完成，标题: {Title}", title);

    _logger.LogInformation("开始登录流程...");

    // 填入手机号
    var phoneInput = page.GetByPlaceholder("输入手机号");
    await phoneInput.FillAsync(_config.PhoneNumber);
    _logger.LogInformation("已输入手机号: {Phone}", _config.PhoneNumber);

    // 填入密码
    var passwordInput = page.Locator("#userPassword");
    await passwordInput.FillAsync(_config.Password);
    _logger.LogInformation("已输入密码");

    // 点击确认按钮
    var submitButton = page.Locator("button[type='submit'].btn-action");
    await submitButton.ClickAsync();
    _logger.LogInformation("已点击确认按钮");

    // 等待页面加载完成
    await WaitForPageLoadedAsync(page);
    _logger.LogInformation("登录完成，页面已加载");
  }

  /// <summary>
  /// 等待页面完全加载
  /// </summary>
  public static async Task WaitForPageLoadedAsync(IPage page)
  {
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = PageTimeoutMs });
    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = PageTimeoutMs });
  }
}
