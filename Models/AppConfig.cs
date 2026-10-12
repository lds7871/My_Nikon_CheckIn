namespace NikonCheckIn.Models;

/// <summary>
/// 应用程序配置模型
/// </summary>
public class AppConfig
{
  /// <summary>手机号</summary>
  public string PhoneNumber { get; set; } = string.Empty;

  /// <summary>密码</summary>
  public string Password { get; set; } = string.Empty;

  /// <summary>是否启用无头模式</summary>
  public bool HeadlessMode { get; set; } = true;

  /// <summary>基础等待时间（毫秒）</summary>
  public long BaseWaitTimeMs { get; set; } = 0;

  /// <summary>
  /// 从配置文件加载配置
  /// </summary>
  public static AppConfig LoadFromFile(string configFilePath)
  {
    var config = new AppConfig();

    if (!File.Exists(configFilePath))
    {
      Console.Error.WriteLine($"警告: 配置文件不存在: {configFilePath}");
      return config;
    }

    var lines = File.ReadAllLines(configFilePath, System.Text.Encoding.UTF8);
    foreach (var line in lines)
    {
      if (!line.Contains('=')) continue;
      var parts = line.Split('=', 2);
      if (parts.Length != 2) continue;

      var key = parts[0].Trim();
      var value = parts[1].Trim();

      switch (key)
      {
        case "手机号":
          config.PhoneNumber = value;
          break;
        case "密码":
          config.Password = value;
          break;
        case "无头模式":
          config.HeadlessMode = !value.Equals("F", StringComparison.OrdinalIgnoreCase);
          break;
        case "基础等待时间":
          if (long.TryParse(value, out var waitTime))
            config.BaseWaitTimeMs = waitTime;
          break;
      }
    }

    return config;
  }
}
