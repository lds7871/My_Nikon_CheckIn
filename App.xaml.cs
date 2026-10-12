namespace NikonCheckIn.Gui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// 整体浅色主题
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage())
		{
			Title = "尼康自动签到",
			Width = 1060,
			Height = 660,
			MinimumWidth = 860,
			MinimumHeight = 520
		};
	}
}
