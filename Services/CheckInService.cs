using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace NikonCheckIn.Services;

/// <summary>
/// 签到服务 - 处理尼康网站签到和点赞流程
/// 对应Java版本: ChickIn.java
/// </summary>
public class CheckInService
{
  private readonly ILogger<CheckInService> _logger;
  private readonly Models.AppConfig _config;
  private const string CheckInPageUrl = "https://my.nikon.com.cn/user/level/task";
  private const string HomeUrl = "https://my.nikon.com.cn/";

  public CheckInService(ILogger<CheckInService> logger, Models.AppConfig config)
  {
    _logger = logger;
    _config = config;
  }

  /// <summary>
  /// 执行签到流程
  /// </summary>
  public async Task DoCheckInAsync(IPage page)
  {
    _logger.LogInformation("开始 CheckIn 流程...");

    // 等待页面加载
    await Task.Delay(3000 + (int)_config.BaseWaitTimeMs);

    // 等待 entcheck-data 元素出现并可点击
    await page.WaitForSelectorAsync("#entcheck-data", new() { Timeout = 30000 });
    _logger.LogInformation("已找到 CheckIn 按钮");

    await page.ClickAsync("#entcheck-data");
    _logger.LogInformation("已点击 CheckIn 按钮");

    // 等待2秒
    await Task.Delay(2000 + (int)_config.BaseWaitTimeMs);
    _logger.LogInformation("等待2秒完成");

    // 跳转到首页
    await page.GotoAsync(HomeUrl);
    _logger.LogInformation("已跳转到 {Url}", HomeUrl);

    await LoginService.WaitForPageLoadedAsync(page);
    _logger.LogInformation("页面加载完成");
    await Task.Delay(2000 + (int)_config.BaseWaitTimeMs);

    // 查找 viewers-reaction 元素并点击17次
    _logger.LogInformation("开始点击点赞按钮...");
    for (int i = 0; i < 17; i++)
    {
      var likeElement = page.Locator(".viewers-reaction").First;
      await likeElement.ClickAsync();
      _logger.LogInformation("第 {Count} 次点击完成", i + 1);

      // 每0.3秒点击一次（除了最后一次）
      if (i < 10)
      {
        await Task.Delay(300 + (int)_config.BaseWaitTimeMs);
      }
    }

    _logger.LogInformation("已完成17次点赞点击");
  }
}
