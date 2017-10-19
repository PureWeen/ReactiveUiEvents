using Android.App;
using Android.Widget;
using Android.OS;
using System;

namespace FreshLonelyAndroidProject
{
    [Activity(Label = "FreshLonelyAndroidProject", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);



            TextView view = new TextView(this);
            view.Events().AfterTextChanged.Subscribe();
        }
    }
}

