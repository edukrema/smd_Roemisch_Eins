using Android.App;
using Android.Runtime;

namespace Game.App;

// No cleartext HTTP, see Resources/xml/network_security_config.xml.
[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
