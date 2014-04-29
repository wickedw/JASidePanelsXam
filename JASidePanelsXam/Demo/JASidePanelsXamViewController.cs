using MonoTouch.UIKit;
using System.Drawing;

namespace JASidePanelsXam
{
	public partial class JASidePanelsXamViewController : UIViewController
	{
		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
		
			UILabel label = new UILabel(new RectangleF(0,0,320,50));

            label.Text = "Centre ViewController";
			
            View.AddSubview(label);
		}
		
		public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
		{
			// Return true for supported orientations
			return (toInterfaceOrientation != UIInterfaceOrientation.PortraitUpsideDown);
		}
	}
}

