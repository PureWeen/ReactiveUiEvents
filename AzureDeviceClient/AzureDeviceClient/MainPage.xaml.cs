using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace AzureDeviceClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            Grid grid = new Grid();
            grid.Events().ChildAdded.Subscribe();
            new TimePicker().Events().Focused.Subscribe();
        }
    }
}
