package com.nikon;

import org.openqa.selenium.edge.EdgeDriver;
import org.openqa.selenium.edge.EdgeOptions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.Console;
import java.nio.charset.Charset;
import java.nio.file.Paths;

public class Main {
  static {
    configureConsoleCharset();
  }

  private static final Logger logger = LoggerFactory.getLogger(Main.class);

  public static void main(String[] args) {
    logger.info("开始执行组合流程: 一个浏览器会话内，先登录，再执行 CheckIn...");

    String driverPath = getEdgeDriverPath();
    logger.info("使用 Edge 驱动: {}", driverPath);
    System.setProperty("webdriver.edge.driver", driverPath);

    EdgeOptions options = new EdgeOptions();
    options.addArguments("--start-maximized");

    EdgeDriver driver = new EdgeDriver(options);
    try {
      // 打开登录页面
      logger.info("Edge 浏览器已启动，打开登录页面...");
      driver.get("https://my.nikon.com.cn/user/level/task");

      // 执行登录流程
      logger.info("执行登录流程...");
      OpenAndLogIn.doLogin(driver);

      // 执行 CheckIn 流程（在同一个浏览器会话里）
      logger.info("登录完成，执行 CheckIn 流程...");
      ChickIn.doCheckIn(driver);

      logger.info("所有流程执行完毕，按 Enter 键退出并关闭浏览器...");
      System.in.read();
    } catch (Exception e) {
      logger.error("自动化过程中发生异常:", e);
      System.exit(1);
    } finally {
      driver.quit();
    }
  }

  private static String getEdgeDriverPath() {
    return Paths.get(System.getProperty("user.dir"), "edgedriver_win64", "msedgedriver.exe").toString();
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