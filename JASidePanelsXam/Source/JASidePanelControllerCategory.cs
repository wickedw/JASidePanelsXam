using MonoTouch.UIKit;

namespace JASidePanelsXam
{	
	public static class JASidePanelControllerCategory
	{
		public static JASidePanelController SidePanelController (this UIViewController SidePanelController)
		{
			UIViewController iter = SidePanelController.ParentViewController;
			
			while (iter != null) {
				if (iter.GetType() == typeof(JASidePanelController)) {
					return (JASidePanelController)iter;
				} else if ((iter.ParentViewController != null) && iter.ParentViewController != iter) {
					iter = iter.ParentViewController;
				} else {
					iter = null;
				}		
			}
			
			return null;
		}
	}
}