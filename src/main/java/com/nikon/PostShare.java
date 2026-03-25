package com.nikon;

import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Duration;
import java.util.List;

public class PostShare extends OpenAndLogIn {
  private static final Logger logger = LoggerFactory.getLogger(PostShare.class);
  private static final String POST_URL = "https://my.nikon.com.cn/post/detail/832446";
  private static final Duration PAGE_TIMEOUT = Duration.ofSeconds(30);

  public static void doPostShare(WebDriver driver) throws Exception {
    logger.info("开始帖子分享流程...");

    // 跳转到指定页面
    driver.get(POST_URL);
    logger.info("已跳转到 {}", POST_URL);

    // 等待页面加载
    waitForPageLoaded(driver);
    logger.info("页面加载完成");
    long baseWaitTime = loadBaseWaitTime();
    Thread.sleep(2000 + baseWaitTime);

    // 点击第三个 class="ic-external" 的元素
    List<WebElement> externalButtons = driver.findElements(By.className("ic-external"));
    if (externalButtons.size() >= 3) {
      WebElement thirdExternalButton = externalButtons.get(2); // 索引 2 是第三个
      thirdExternalButton.click();
      logger.info("已点击第三个 ic-external 按钮");
    } else {
      logger.warn("找不到第三个 ic-external 按钮，当前仅有 {} 个", externalButtons.size());
    }

    // 等待弹窗出现
    Thread.sleep(1000 + baseWaitTime);

    // 点击 class="modal-share_icon modal-share_icon-wechat" 的元素
    WebElement wechatShareButton = driver.findElement(By.className("modal-share_icon-wechat"));
    wechatShareButton.click();
    logger.info("已点击微信分享按钮");

    Thread.sleep(1000 + baseWaitTime);

    // 点击第一个 class="modal-share_icon" 的元素
    List<WebElement> shareIcons = driver.findElements(By.className("modal-share_icon"));
    if (shareIcons.size() > 0) {
      WebElement firstShareIcon = shareIcons.get(0);
      firstShareIcon.click();
      logger.info("已点击第一个 modal-share_icon 按钮");
    } else {
      logger.warn("找不到 modal-share_icon 按钮");
    }

    logger.info("帖子分享流程完成");
  }
}
