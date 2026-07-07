using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

namespace NikonCheckIn.Services;

/// <summary>
/// 评论服务 - 对指定帖子执行评论流程
/// 对应Java版本: PostComment.java
/// </summary>
public class CommentService
{
  private readonly ILogger<CommentService> _logger;
  private readonly Models.AppConfig _config;
  private const string PostUrl = "https://my.nikon.com.cn/post/detail/833001";

  public CommentService(ILogger<CommentService> logger, Models.AppConfig config)
  {
    _logger = logger;
    _config = config;
  }

  /// <summary>
  /// 执行评论流程 - 循环5次评论
  /// </summary>
  public async Task DoCommentAsync(IPage page)
  {
    _logger.LogInformation("开始评论流程...");

    // 跳转到指定URL
    await page.GotoAsync(PostUrl);
    _logger.LogInformation("已跳转到 {Url}", PostUrl);

    await LoginService.WaitForPageLoadedAsync(page);
    _logger.LogInformation("页面加载完成");
    await Task.Delay(2000 + (int)_config.BaseWaitTimeMs);

    // 循环执行5次评论
    for (int i = 0; i < 5; i++)
    {
      _logger.LogInformation("开始第 {Count} 次评论", i + 1);

      // 点击评论按钮
      var commentButton = page.Locator(".btn_rect.btn_rect_b.btn_fill_linear.txt_14");
      await commentButton.ScrollIntoViewIfNeededAsync();
      await Task.Delay(500 + (int)_config.BaseWaitTimeMs);
      await commentButton.ClickAsync();
      _logger.LogInformation("已点击评论按钮");

      // 等待0.3秒
      await Task.Delay(300 + (int)_config.BaseWaitTimeMs);

      // 查找textarea并输入内容
      var textArea = page.Locator(".el-textarea__inner");
      await textArea.ClearAsync();
      await textArea.FillAsync("JY+5");
      _logger.LogInformation("已输入评论内容");

      // 点击提交按钮
      var submitButton = page.Locator(".postnavbar_menu_text");
      await submitButton.ScrollIntoViewIfNeededAsync();
      await Task.Delay(500 + (int)_config.BaseWaitTimeMs);
      await submitButton.ClickAsync();
      _logger.LogInformation("已点击提交按钮");

      // 等待1秒
      await Task.Delay(1000 + (int)_config.BaseWaitTimeMs);

      _logger.LogInformation("第 {Count} 次评论完成", i + 1);
    }

    _logger.LogInformation("评论流程完成");
  }
}
