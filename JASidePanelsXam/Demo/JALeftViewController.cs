using System;
using System.Drawing;
using System.Reflection;
using MonoTouch.UIKit;

namespace JASidePanelsXam
{
	public partial class JALeftViewController : UIViewController
	{
		public UILabel Label {get; set;}
		
		public UIButton Hide {get; set;}

		public UIButton Show {get; set;}
		
		public UIButton RemoveRightPanel {get; set;}
		
		public UIButton AddRightPanel {get; set;}
		
		public UIButton ChangeCenterPanel {get; set;}

	    public JACenterViewController JaCenterViewController { get; set; }

	    public override void ViewDidLoad ()
		{
			base.ViewDidLoad();

			this.View.BackgroundColor = UIColor.Blue;

			UILabel label = new UILabel();
			label.Font = UIFont.BoldSystemFontOfSize(20.0f);
			label.TextColor = UIColor.White;
			label.BackgroundColor = UIColor.Clear;
			label.Text = "Left Panel";
			label.SizeToFit();
			label.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleBottomMargin;
			this.View.AddSubview(label);
			this.Label = label;

			UIButton button = UIButton.FromType(UIButtonType.RoundedRect);
			button.Frame = new RectangleF(20.0f, 70.0f, 200.0f, 40.0f);
			button.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleBottomMargin;
			button.SetTitle("Hide Center", UIControlState.Normal);
			button.TouchUpInside+= (sender, e) => { _hideTapped(); };
			this.View.AddSubview(button);
			this.Hide = button;

			button = UIButton.FromType(UIButtonType.RoundedRect);
			button.Frame = this.Hide.Frame;
			button.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleBottomMargin;
			button.SetTitle("Show Center", UIControlState.Normal);
			button.TouchUpInside+= (sender, e) => { _showTapped(); };
			button.Hidden = true;
			this.View.AddSubview(button);
			this.Show = button;

			button = UIButton.FromType(UIButtonType.RoundedRect);
			button.Frame = new RectangleF(20.0f, 120.0f, 200.0f, 40.0f);
			button.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleBottomMargin;
			button.SetTitle("Remove Right Panel", UIControlState.Normal);
			button.TouchUpInside+= (sender, e) => { _removeRightPanelTapped(); };
			this.View.AddSubview(button);

			this.RemoveRightPanel = button;
			button = UIButton.FromType(UIButtonType.RoundedRect);
			button.Frame = this.RemoveRightPanel.Frame;
			button.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleBottomMargin;
			button.SetTitle("Add Right Panel", UIControlState.Normal);
			button.TouchUpInside+= (sender, e) => { _addRightPanelTapped(); };
			button.Hidden = true;
			this.View.AddSubview(button);
			this.AddRightPanel = button;

			button = UIButton.FromType(UIButtonType.RoundedRect);
			button.Frame = new RectangleF(20.0f, 170.0f, 200.0f, 40.0f);
			button.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			button.SetTitle("Change Center Panel", UIControlState.Normal);
			button.TouchUpInside+= (sender, e) => { _changeCenterPanelTapped(); };
			this.View.AddSubview(button);
			this.ChangeCenterPanel = button;
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
			this.Label.Center = new PointF((float)Math.Floor(this.SidePanelController().LeftVisibleWidth / 2.0f), 25.0f);
		}
		
		void _hideTapped()
		{
			this.SidePanelController().SetCenterPanel(true, true, 0.2f);
			this.Hide.Hidden = true;
			this.Show.Hidden = false;
		}
		
		void _showTapped()
		{
			this.SidePanelController().SetCenterPanel(false, true, 0.2f);
			this.Hide.Hidden = false;
			this.Show.Hidden = true;
		}
		
		void _removeRightPanelTapped()
		{
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

			this.SidePanelController().RightPanelViewController = null;
			this.RemoveRightPanel.Hidden = true;
			this.AddRightPanel.Hidden = false;
		}
		
		void _addRightPanelTapped()
        {
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            this.SidePanelController().CenterPanelViewController =
		        new UINavigationController(new JACenterViewController {WillReload = true});

			this.RemoveRightPanel.Hidden = false;
			this.AddRightPanel.Hidden = true;
		}
		
		void _changeCenterPanelTapped()
		{
			this.SidePanelController().CenterPanelViewController = new UINavigationController(new JACenterViewController());
		}
	}
}
