using System;
using System.Reflection;
using MonoTouch.UIKit;

namespace JASidePanelsXam
{
    public partial class JACenterViewController : UIViewController
    {
        public JACenterViewController()
        {
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            Title = "Center Panel";

            WillReload = true;
        }

        public bool WillReload { get; set; }

        public override void ViewDidLoad()
        {
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            base.ViewDidLoad();

			DateTime seed = DateTime.Now;
			Random r = new Random (seed.Millisecond);

            int red = r.Next(255);
            int green = r.Next(255);
            int blue = r.Next(255);

			this.View.BackgroundColor = UIColor.FromRGB(red, green, blue);

			this.SidePanelController().RightPanelViewController = new JARightViewController();
		}

        public override void ViewWillAppear(bool animated)
        {
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            base.ViewWillAppear(animated);

            // Always reload the right panel
            if (WillReload) ReloadRightPanel();
        }

		public void ClearRight()
		{
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            this.SidePanelController().RightPanelViewController = null;
		}

		public void ReloadRightPanel()
		{
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);
            
            this.SidePanelController().RightPanelViewController = new JARightViewController();
		}
    }

}

