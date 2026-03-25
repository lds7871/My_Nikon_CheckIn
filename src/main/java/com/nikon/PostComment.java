package com.nikon;

import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Duration;

public class PostComment extends OpenAndLogIn {
  private static final Logger logger = LoggerFactory.getLogger(PostComment.class);
  private static final String POST_URL = "https://my.nikon.com.cn/post/detail/833001";
  private static final Duration PAGE_TIMEOUT = Duration.ofSeconds(30);

  public static void doComment(WebDriver driver) throws Exception {
    logger.info("开始评论流程...");

    // 跳转到指定URL
    driver.get(POST_URL);
    logger.info("已跳转到 {}", POST_URL);

    waitForPageLoaded(driver);
    logger.info("页面加载完成");
    long baseWaitTime = loadBaseWaitTime();
    Thread.sleep(2000 + baseWaitTime);

    // 循环执行5次评论
    for (int i = 0; i < 5; i++) {
      logger.info("开始第 {} 次评论", i + 1);

      // 点击评论按钮
      WebElement commentButton = driver.findElement(
          By.cssSelector(".btn_rect.btn_rect_b.btn_fill_linear.txt_14"));
      // 滚动到按钮使其可见
      ((JavascriptExecutor) driver).executeScript("arguments[0].scrollIntoView(true);", commentButton);
      Thread.sleep(500 + baseWaitTime);
      commentButton.click();
      logger.info("已点击评论按钮");

      // 线程等待0.3秒
      Thread.sleep(300 + baseWaitTime);

      // 查找元素并输入内容
      WebElement textArea = driver.findElement(By.className("el-textarea__inner"));
      textArea.clear();
      textArea.sendKeys("JY+5");
      logger.info("已输入评论内容");

      // 点击提交按钮
      WebElement submitButton = driver.findElement(By.className("postnavbar_menu_text"));
      // 滚动到按钮使其可见
      ((JavascriptExecutor) driver).executeScript("arguments[0].scrollIntoView(true);", submitButton);
      Thread.sleep(500 + baseWaitTime);
      submitButton.click();
      logger.info("已点击提交按钮");

      // 线程等待1秒
      Thread.sleep(1000 + baseWaitTime);

      logger.info("第 {} 次评论完成", i + 1);
    }

    logger.info("评论流程完成");
  }
}
