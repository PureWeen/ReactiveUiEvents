using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace OldRxUI.PCL
{
    public class OldRxUIPCL : ContentPage
	{
		public OldRxUIPCL()
		{
			var button = new Button
            {
                Text = "Click Me!",
                VerticalOptions = LayoutOptions.CenterAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
            };

            int clicked = 0;
            button.Events().ChildrenReordered.Subscribe();

			        Content = button;
		}
	}
}
