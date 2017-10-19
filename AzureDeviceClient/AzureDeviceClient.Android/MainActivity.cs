using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using ReactiveUI;
using ReactiveUI.Events;
using Android.App;
using Android.Widget;
using Android.OS;
using System;

namespace AzureDeviceClient.Droid
{
    [Activity(Label = "AzureDeviceClient", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());

             
            TextView view = new TextView(this);

            AzureIoTSuiteRemoteMonitoringHelper.RemoteMonitoringDevice.Instance.Connect().Wait();

            Microsoft.Azure.Devices.Client.Message message = 
                new Microsoft.Azure.Devices.Client.Message(new byte[1999]);
            //view.Events()
        }
    }
}

