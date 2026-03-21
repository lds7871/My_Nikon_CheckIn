package com.nikon;

import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.edge.EdgeDriver;
import org.openqa.selenium.edge.EdgeOptions;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class ChickIn extends OpenAndLogIn {
  private static final Logger logger = LoggerFactory.getLogger(ChickIn.class);
  private static final String TARGET_URL = "https://my.nikon.com.cn/user/level/task";
  private static final String CHECKIN_URL = "https://my.nikon.com.cn/";
  private static final Duration PAGE_TIMEOUT = Duration.ofSeconds(30);

  public static void main(String[] args) {
    run(true);
  }

  public static void run(boolean waitForExit) {
    logger.info("启动 CheckIn 自动化程序...");

    String driverPath = getEdgeDriverPath();
    logger.info("使用 Edge 驱动: {}", driverPath);
    System.setProperty("webdriver.edge.driver", driverPath);

    EdgeOptions options = new EdgeOptions();
    options.addArguments("--headless");
    options.addArguments("--start-maximized");

    EdgeDriver driver = new EdgeDriver(options);
    try {
      // 打开登录页面
      logger.info("Edge 浏览器已启动，打开页面 {}", TARGET_URL);
      driver.get(TARGET_URL);

      loadCredentialsAndLogin(driver);

      // CheckIn 流程开始
      doCheckIn(driver);

      if (waitForExit) {
        logger.info("CheckIn 流程完成，程序即将关闭...");
      }
    } catch (Exception e) {
      logger.error("自动化过程中发生异常:", e);
      System.exit(1);
    } finally {
      driver.quit();
    }
  }

  public static void doCheckIn(WebDriver driver) throws Exception {
    logger.info("开始 CheckIn 流程...");

    // 等待页面加载
    Thread.sleep(3000);
    // 等待 entcheck-dat 元素出现并可点击
    WebDriverWait wait = new WebDriverWait(driver, PAGE_TIMEOUT);
    WebElement checkInButton = wait.until(
        webDriver -> webDriver.findElement(By.id("entcheck-data")));
    logger.info("已找到 CheckIn 按钮");

    checkInButton.click();
    logger.info("已点击 CheckIn 按钮");

    // 线程等待2秒
    Thread.sleep(2000);
    logger.info("等待2秒完成");

    // 跳转到指定URL
    driver.get(CHECKIN_URL);
    logger.info("已跳转到 {}", CHECKIN_URL);

    waitForPageLoaded(driver);
    logger.info("页面加载完成");
    Thread.sleep(2000);

    // 查找第一个 span 元素并点击11次
    logger.info("开始点击点赞按钮...");
    for (int i = 0; i < 11; i++) {
      WebElement likeElement = driver.findElement(By.xpath("//span[@class='ic-like']"));
      likeElement.click();
      logger.info("第 {} 次点击完成", i + 1);

      // 每0.3秒点击一次（除了最后一次）
      if (i < 10) {
        Thread.sleep(300);
      }
    }

    logger.info("已完成11次点击");

  }

  private static void loadCredentialsAndLogin(WebDriver driver) throws Exception {
    waitForPageLoaded(driver);
    logger.info("页面加载完成");

    // 读取账号密码配置
    Map<String, String> credentials = loadCredentials();
    String phoneNumber = credentials.get("手机号");
    String password = credentials.get("密码");

    logger.info("开始登录流程...");

    // 填入手机号
    WebElement phoneInput = driver.findElement(By.xpath("//input[@placeholder='输入手机号']"));
    phoneInput.sendKeys(phoneNumber);
    logger.info("已输入手机号");

    // 填入密码
    WebElement passwordInput = driver.findElement(By.id("userPassword"));
    passwordInput.sendKeys(password);
    logger.info("已输入密码");

    // 点击确认按钮
    WebElement submitButton = driver
        .findElement(By.xpath("//button[@type='submit' and contains(@class, 'btn-action')]"));
    submitButton.click();
    logger.info("已点击确认按钮");

    // 等待页面加载
    waitForPageLoaded(driver);
    logger.info("登录完成");
  }

  private static void waitForPageLoaded(WebDriver driver) {
    new WebDriverWait(driver, PAGE_TIMEOUT).until(
        webDriver -> ((JavascriptExecutor) webDriver)
            .executeScript("return document.readyState").equals("complete"));
  }

  private static String getEdgeDriverPath() {
    return Paths.get(System.getProperty("user.dir"), "edgedriver_win64", "msedgedriver.exe").toString();
  }

  private static Map<String, String> loadCredentials() throws Exception {
    Map<String, String> credentials = new HashMap<>();
    String configFile = Paths.get(System.getProperty("user.dir"), "账号密码配置.txt").toString();

    Files.readAllLines(Paths.get(configFile), StandardCharsets.UTF_8).forEach(line -> {
      if (line.contains("=")) {
        String[] parts = line.split("=", 2);
        if (parts.length == 2) {
          credentials.put(parts[0].trim(), parts[1].trim());
        }
      }
    });

    return credentials;
  }
}
