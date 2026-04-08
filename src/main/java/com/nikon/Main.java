package com.nikon;

import org.openqa.selenium.edge.EdgeDriver;
import org.openqa.selenium.edge.EdgeOptions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.Console;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;

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

    // 读取配置
    Map<String, String> config = loadConfig();
    boolean headlessMode = isHeadlessModeEnabled(config);
    logger.info("无头模式: {}", headlessMode ? "启用" : "禁用");

    EdgeOptions options = new EdgeOptions();
    if (headlessMode) {
      options.addArguments("--headless");
    }
    options.addArguments("--start-maximized");

    EdgeDriver driver = new EdgeDriver(options);
    try {
      // ============打开登录页面
      logger.info("Edge 浏览器已启动，打开登录页面...");
      driver.get("https://my.nikon.com.cn/user/level/task");

      // ============执行登录流程
      logger.info("执行登录流程...");
      OpenAndLogIn.doLogin(driver);

      // CheckIn 流程
      logger.info("登录完成，执行 CheckIn 流程...");
      ChickIn.doCheckIn(driver);

      // ===========帖子分享流程--暂时关闭
      // logger.info("CheckIn 完成，执行帖子分享流程...");
      // PostShare.doPostShare(driver);

      // ============评论流程
      logger.info("CheckIn 完成，执行评论流程...");
      PostComment.doComment(driver);
      // ============

      logger.info("所有流程执行完毕，程序即将关闭...");
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

  private static Map<String, String> loadConfig() {
    Map<String, String> config = new HashMap<>();
    try {
      String configFile = Paths.get(System.getProperty("user.dir"), "账号密码配置.txt").toString();
      Files.readAllLines(Paths.get(configFile), StandardCharsets.UTF_8).forEach(line -> {
        if (line.contains("=")) {
          String[] parts = line.split("=", 2);
          if (parts.length == 2) {
            config.put(parts[0].trim(), parts[1].trim());
          }
        }
      });
    } catch (Exception e) {
      logger.warn("读取配置文件失败，使用默认配置", e);
    }
    return config;
  }

  private static boolean isHeadlessModeEnabled(Map<String, String> config) {
    String headlessValue = config.getOrDefault("无头模式", "T");
    return !headlessValue.equalsIgnoreCase("F");
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