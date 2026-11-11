using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace SelfLearn.Mobile.Android
{
    // NetworkSecurityConfig: oddiy http faqat dev-kompyuter manzillariga (Resources/xml/network_security_config.xml).
    // AllowBackup=false: kirish tokeni Google zaxira nusxasiga tushib ketmasin.
    [Application(NetworkSecurityConfig = "@xml/network_security_config", AllowBackup = false)]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }
    }
}
