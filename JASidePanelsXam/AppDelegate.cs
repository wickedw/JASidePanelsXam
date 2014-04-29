using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System;

namespace JASidePanelsXam
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        UIWindow window;
        
        public static JASidePanelController jaSidePanelController { get; set; }

        public AppDelegate()
        {
             jaSidePanelController = new JASidePanelController();
        }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            window = new UIWindow(UIScreen.MainScreen.Bounds);

            JALeftViewController jaLeftViewController = new JALeftViewController();
            jaLeftViewController.JaCenterViewController = new JACenterViewController();

            jaSidePanelController.ShouldDelegateAutorotateToVisiblePanel = false;
			jaSidePanelController.CenterPanelViewController =  new UINavigationController(jaLeftViewController.JaCenterViewController);
			Console.WriteLine (jaSidePanelController.CenterPanelViewController.ParentViewController == null);

			jaSidePanelController.LeftPanelViewController = jaLeftViewController;
			Console.WriteLine (jaSidePanelController.LeftPanelViewController.ParentViewController == null);

			jaSidePanelController.RightPanelViewController = new UINavigationController(new JARightViewController()); 
			Console.WriteLine (jaSidePanelController.RightPanelViewController.ParentViewController == null);

			jaSidePanelController.ShouldResizeLeftPanel = true;

			window.RootViewController = jaSidePanelController;
            window.MakeKeyAndVisible();
            return true;
        }
    }
}


