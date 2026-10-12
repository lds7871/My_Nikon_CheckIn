using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace NikonCheckIn.Services;

/// <summary>
/// 分享服务 - 对指定帖子执行分享流程（当前已关闭）
/// 对应Java版本: PostShare.java
/// </summary>
public class ShareService
{
  private readonly ILogger<ShareService> _logger;
  private readonly Models.AppConfig _config;
  private const string PostUrl = "https://my.nikon.com.cn/post/detail/832446";

  public ShareService(ILogger<ShareService> logger, Models.AppConfig config)
  {
    _logger = logger;
    _config = config;
  }

  /// <summary>
  /// 执行帖子分享流程
  /// </summary>
  public async Task DoPostShareAsync(IPage page)
  {
    _logger.LogInformation("开始帖子分享流程...");

    // 跳转到指定页面
    await page.GotoAsync(PostUrl);
    _logger.LogInformation("已跳转到 {Url}", PostUrl);

    // 等待页面加载
    await LoginService.WaitForPageLoadedAsync(page);
    _logger.LogInformation("页面加载完成");
    await Task.Delay(2000 + (int)_config.BaseWaitTimeMs);

    // 点击第三个 ic-external 元素
    var externalButtons = page.Locator(".ic-external");
    var buttonCount = await externalButtons.CountAsync();
    if (buttonCount >= 3)
    {
      var thirdButton = externalButtons.Nth(2);
      await thirdButton.ClickAsync();
      _logger.LogInformation("已点击第三个 ic-external 按钮");
    }
    else
    {
      _logger.LogWarning("找不到第三个 ic-external 按钮，当前仅有 {Count} 个", buttonCount);
    }

    // 等待弹窗出现
    await Task.Delay(1000 + (int)_config.BaseWaitTimeMs);

    // 点击微信分享按钮
    var wechatShareButton = page.Locator(".modal-share_icon-wechat");
    await wechatShareButton.ClickAsync();
    _logger.LogInformation("已点击微信分享按钮");

    await Task.Delay(1000 + (int)_config.BaseWaitTimeMs);

    // 点击第一个 modal-share_icon 元素
    var shareIcons = page.Locator(".modal-share_icon");
    var shareIconCount = await shareIcons.CountAsync();
    if (shareIconCount > 0)
    {
      var firstShareIcon = shareIcons.First;
      await firstShareIcon.ClickAsync();
      _logger.LogInformation("已点击第一个 modal-share_icon 按钮");
    }
    else
    {
      _logger.LogWarning("找不到 modal-share_icon 按钮");
    }

    _logger.LogInformation("帖子分享流程完成");
  }
}
