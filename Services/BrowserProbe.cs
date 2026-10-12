using System.Diagnostics;

namespace NikonCheckIn.Gui.Services;

/// <summary>本机检测到的 Chromium 系浏览器</summary>
public sealed record DetectedBrowser(string Name, string Channel, string Path, string? Version);

/// <summary>
/// 浏览器适配检查：查找本机可被 Playwright 使用的 Chrome / Edge，
/// 以及 Playwright 内置 Chromium 是否已下载。
/// </summary>
public static class BrowserProbe
{
	/// <summary>Playwright 支持的启动通道顺序（与原有控制台程序一致）</summary>
	public static readonly string[] DefaultChannelOrder =
	{
		"chrome", "msedge", "chrome-beta", "msedge-beta", "chrome-dev", "msedge-dev"
	};

	private static readonly (string Name, string Channel, string[] Paths)[] Candidates =
	{
		("Google Chrome", "chrome", new[]
		{
			@"C:\Program Files\Google\Chrome\Application\chrome.exe",
			@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
			Path.Combine(LocalAppData, @"Google\Chrome\Application\chrome.exe")
		}),
		("Google Chrome Beta", "chrome-beta", new[]
		{
			@"C:\Program Files\Google\Chrome Beta\Application\chrome.exe",
			@"C:\Program Files (x86)\Google\Chrome Beta\Application\chrome.exe",
			Path.Combine(LocalAppData, @"Google\Chrome Beta\Application\chrome.exe")
		}),
		("Microsoft Edge", "msedge", new[]
		{
			@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
			@"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
		}),
		("Microsoft Edge Beta", "msedge-beta", new[]
		{
			@"C:\Program Files (x86)\Microsoft\Edge Beta\Application\msedge.exe",
			@"C:\Program Files\Microsoft\Edge Beta\Application\msedge.exe"
		})
	};

	private static string LocalAppData =>
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	/// <summary>检测本机安装的 Chromium 系浏览器</summary>
	public static IReadOnlyList<DetectedBrowser> Detect()
	{
		var found = new List<DetectedBrowser>();

		foreach (var (name, channel, paths) in Candidates)
		{
			var path = paths.FirstOrDefault(File.Exists);
			if (path is null)
			{
				continue;
			}

			found.Add(new DetectedBrowser(name, channel, path, ReadVersion(path)));
		}

		return found;
	}

	/// <summary>Playwright 内置 Chromium 是否已下载（未下载时无法使用无 Channel 兜底启动）</summary>
	public static bool HasBundledChromium()
	{
		var root = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
		if (string.IsNullOrWhiteSpace(root))
		{
			root = Path.Combine(LocalAppData, "ms-playwright");
		}

		if (!Directory.Exists(root))
		{
			return false;
		}

		try
		{
			return Directory.EnumerateDirectories(root, "chromium*").Any();
		}
		catch (IOException)
		{
			return false;
		}
	}

	/// <summary>已检测到的浏览器优先，其余按默认顺序补齐</summary>
	public static string[] ChannelOrder(IReadOnlyList<DetectedBrowser> detected)
	{
		var preferred = detected.Select(b => b.Channel).ToList();
		return preferred.Concat(DefaultChannelOrder.Where(c => !preferred.Contains(c))).ToArray();
	}

	private static string? ReadVersion(string exePath)
	{
		try
		{
			var info = FileVersionInfo.GetVersionInfo(exePath);
			return info.ProductVersion ?? info.FileVersion;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}
}
