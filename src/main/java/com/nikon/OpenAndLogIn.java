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

import java.io.Console;
import java.nio.charset.StandardCharsets;
import java.nio.charset.Charset;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.HashMap;
import java.util.Map;

public class OpenAndLogIn {
  static {
    configureConsoleCharset();
  }

  private static final Logger logger = LoggerFactory.getLogger(OpenAndLogIn.class);
  private static final String TARGET_URL = "https://my.nikon.com.cn/user/level/task";
  private static final Duration PAGE_TIMEOUT = Duration.ofSeconds(30);

  public static void main(String[] args) {
    run(true);
  }

  public static void run(boolean waitForExit) {
    logger.info("启动 Selenium 浏览器自动化程序...");

    String driverPath = getEdgeDriverPath();
    logger.info("使用 Edge 驱动: {}", driverPath);
    System.setProperty("webdriver.edge.driver", driverPath);

    EdgeOptions options = new EdgeOptions();
    options.addArguments("--headless");
    options.addArguments("--start-maximized");

    EdgeDriver driver = new EdgeDriver(options);
    try {
      logger.info("Edge 浏览器已启动，打开页面 {}", TARGET_URL);
      driver.get(TARGET_URL);

      doLogin(driver);

      if (waitForExit) {
        waitForUserExit();
      }
    } catch (Exception e) {
      logger.error("自动化过程中发生异常:", e);
      System.exit(1);
    } finally {
      driver.quit();
    }
  }

  public static void doLogin(WebDriver driver) throws Exception {
    waitForPageLoaded(driver);
    logger.info("页面加载完成，标题: {}", driver.getTitle());

    // 读取账号密码配置
    Map<String, String> credentials = loadCredentials();
    String phoneNumber = credentials.get("手机号");
    String password = credentials.get("密码");

    logger.info("开始登录流程...");

    // 填入手机号
    WebElement phoneInput = driver.findElement(By.xpath("//input[@placeholder='输入手机号']"));
    phoneInput.sendKeys(phoneNumber);
    logger.info("已输入手机号: {}", phoneNumber);

    // 填入密码
    WebElement passwordInput = driver.findElement(By.id("userPassword"));
    passwordInput.sendKeys(password);
    logger.info("已输入密码");

    // 点击确认按钮
    WebElement submitButton = driver
        .findElement(By.xpath("//button[@type='submit' and contains(@class, 'btn-action')]"));
    submitButton.click();
    logger.info("已点击确认按钮");

    // 等待页面加载完成
    waitForPageLoaded(driver);
    logger.info("登录完成，页面已加载");
  }

  protected static void waitForPageLoaded(WebDriver driver) {
    new WebDriverWait(driver, PAGE_TIMEOUT).until(
        webDriver -> ((JavascriptExecutor) webDriver)
            .executeScript("return document.readyState").equals("complete"));
  }

  protected static long loadBaseWaitTime() {
    try {
      Map<String, String> config = loadCredentials();
      String baseWaitTimeStr = config.get("基础等待时间");
      if (baseWaitTimeStr != null && !baseWaitTimeStr.isBlank()) {
        return Long.parseLong(baseWaitTimeStr.trim());
      }
    } catch (Exception e) {
      logger.warn("读取基础等待时间失败，使用默认值 0", e);
    }
    return 0;
  }

  protected static String getEdgeDriverPath() {
    return Paths.get(System.getProperty("user.dir"), "edgedriver_win64", "msedgedriver.exe").toString();
  }

  private static void waitForUserExit() throws Exception {
    logger.info("登录流程完成，程序即将关闭...");
  }

  protected static Map<String, String> loadCredentials() throws Exception {
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

  private static void configureConsoleCharset() {
    String consoleCharset = resolveConsoleCharset();
    System.setProperty("nikon.console.charset", consoleCharset);
  }

  private static String resolveConsoleCharset() {
    Console console = System.console();
    if (console != null) {
      Charset consoleCharset = console.charset();
      if (consoleCharset != null) {
        return consoleCharset.name();
      }
    }

    String[] candidates = { "stdout.encoding", "sun.stdout.encoding", "native.encoding", "sun.jnu.encoding" };
    for (String candidate : candidates) {
      String value = System.getProperty(candidate);
      if (value != null && !value.isBlank()) {
        return value;
      }
    }

    return Charset.defaultCharset().name();
  }
}
