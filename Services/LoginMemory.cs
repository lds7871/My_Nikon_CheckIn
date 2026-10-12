using System.Text;

namespace NikonCheckIn.Gui.Services;

/// <summary>
/// 记住上次输入的账号密码，下次启动自动填充（明文保存，仅存在本机当前用户目录）。
/// 存放位置：%LOCALAPPDATA%\NikonCheckIn\登录信息.txt
/// 如果不想让账号密码落盘，把输入框留空并改用环境变量 NK-ZH / NK-PW。
/// </summary>
public sealed class LoginMemory
{
	private const string PhoneKey = "手机号";
	private const string PasswordKey = "密码";

	private static readonly string DirectoryPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"NikonCheckIn");

	private static readonly string FilePath = Path.Combine(DirectoryPath, "登录信息.txt");

	public string PhoneNumber { get; set; } = string.Empty;

	public string Password { get; set; } = string.Empty;

	/// <summary>保存时间（本地时间）</summary>
	public DateTime SavedAt { get; set; }

	/// <summary>存档路径（用于日志提示）</summary>
	public static string Location => FilePath;

	public bool IsEmpty => string.IsNullOrWhiteSpace(PhoneNumber) && string.IsNullOrWhiteSpace(Password);

	/// <summary>读取上次保存的账号密码；没有存档或读不出来时返回 null</summary>
	public static LoginMemory? Load()
	{
		try
		{
			if (!File.Exists(FilePath))
			{
				return null;
			}

			var memory = new LoginMemory { SavedAt = File.GetLastWriteTime(FilePath) };

			foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
			{
				if (!line.Contains('='))
				{
					continue;
				}

				var parts = line.Split('=', 2);
				if (parts.Length != 2)
				{
					continue;
				}

				var key = parts[0].Trim();
				var value = parts[1].Trim();

				if (key == PhoneKey)
				{
					memory.PhoneNumber = value;
				}
				else if (key == PasswordKey)
				{
					memory.Password = value;
				}
			}

			return memory;
		}
		catch (Exception)
		{
			// 存档异常时按“没有记录”处理
			return null;
		}
	}

	public void Save()
	{
		Directory.CreateDirectory(DirectoryPath);

		var lines = new List<string>
		{
			$"{PhoneKey}={PhoneNumber}",
			$"{PasswordKey}={Password}"
		};

		File.WriteAllLines(FilePath, lines, Encoding.UTF8);
	}
}
