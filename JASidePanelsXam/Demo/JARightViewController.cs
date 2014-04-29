using System;
using System.Drawing;
using MonoTouch.UIKit;

namespace JASidePanelsXam
{
    public partial class JARightViewController : UIViewController
    {
        public UILabel Label { get; set; }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.View.BackgroundColor = UIColor.Red;

            UILabel label = new UILabel();
            label.Font = UIFont.BoldSystemFontOfSize(20.0f);
            label.Text = "Right Panel";
            label.TextColor = UIColor.White;
            label.BackgroundColor = UIColor.Clear;
            label.SizeToFit();
            label.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleBottomMargin;
            this.View.AddSubview(label);
			View.AddSubview(label);
			this.Label = label;
		}

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            this.Label.Center = new PointF((float)Math.Floor((this.View.Bounds.Size.Width - this.SidePanelController().RightVisibleWidth) + this.SidePanelController().RightVisibleWidth / 2.0f), 25.0f);
        }
    }
}

