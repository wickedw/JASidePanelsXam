using System;
using System.Reflection;
using MonoTouch.ObjCRuntime;
using TimeInterval = System.Double;
using MonoTouch.UIKit;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.CoreAnimation;

namespace JASidePanelsXam
{
    public class JASidePanelController : UIViewController //, UIGestureRecognizerDelegate
    {
        protected bool DebugOutput { get; set; }

        protected RectangleF _centerPanelRestingFrame;
        protected PointF _locationBeforePan;
        private UIImage _defaultImage;

        // http://stackoverflow.com/questions/12719864/best-practices-for-context-parameter-in-addobserver-kvo
        static IntPtr ja_kvoContext = new IntPtr(0);

        public enum JASidePanelStyle
        {
            SingleActive = 0,
            MultipleActive
        }

        public enum JASidePanelState
        {
            CenterVisible = 1,
            LeftVisible,
            RightVisible
        }

        UIViewController _leftPanelViewController;
        UIViewController _centerPanelViewController;
        UIViewController _rightPanelViewController;
        JASidePanelStyle _style;

        public float LeftVisibleWidth
        {
            get
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (CenterPanelHidden && ShouldResizeLeftPanel)
                {
                    return View.Bounds.Size.Width;
                }
                else
                {
                    // Return LeftFixedWidth if defined, else calculate based on GapPercentage
                    return LeftFixedWidth != 0 ? LeftFixedWidth : (float)Math.Floor(View.Bounds.Size.Width * LeftGapPercentage);
                }

            }
        }

        public float RightVisibleWidth
        {
            get
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (CenterPanelHidden && ShouldResizeRightPanel)
                {
                    return View.Bounds.Size.Width;
                }
                else
                {
                    return RightFixedWidth != 0 ? RightFixedWidth : (float)Math.Floor(View.Bounds.Size.Width * RightGapPercentage);
                }

            }
        }

        bool _centerPanelHidden;

        public UIViewController VisiblePanelViewController { get; set; }

        UIView _tapView;
        public UIView LeftPanelContainer { get; set; }
        public UIView RightPanelContainer { get; set; }
        public UIView CenterPanelContainer { get; set; }

        public UIView TapView
        {
            get
            {
                return _tapView;
            }
            set
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (value != _tapView)
                {
                    if (_tapView != null)
                    {
                        _tapView.RemoveFromSuperview();
                    }

                    _tapView = value;
                    if (_tapView != null)
                    {
                        _tapView.Frame = CenterPanelContainer.Bounds;
                        _tapView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                        _addTapGestureToView(_tapView);
                        if (RecognizesPanGesture)
                        {
                            _addPanGestureToView(_tapView);
                        }

                        CenterPanelContainer.AddSubview(_tapView);
                    }

                }

            }
        }

        public JASidePanelStyle Style
        {
            get
            {
                return _style;
            }
            set
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (value != _style)
                {
                    _style = value;
                    if (IsViewLoaded)
                    {
                        _configureContainers();
                        _layoutSideContainersDuration(false, 0.0f);
                    }

                }

            }
        }

        public UIImage CustomLeftButtonBarImage { get; set; }
        public JASidePanelState State { get; set; }

        public UIViewController LeftPanelViewController
        {
            get
            {
                return _leftPanelViewController;
            }
            set
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (value != _leftPanelViewController)
                {
                    if (_leftPanelViewController != null)
                    {
                        // http://www.apeth.com/iOSBook/ch19.html#_container_view_controllers
                        _leftPanelViewController.WillMoveToParentViewController(null);
                        _leftPanelViewController.View.RemoveFromSuperview();
                        _leftPanelViewController.RemoveFromParentViewController();
                    }

                    _leftPanelViewController = value;

                    if (_leftPanelViewController != null)
                    {
                        AddChildViewController(_leftPanelViewController);
                        _leftPanelViewController.DidMoveToParentViewController(this);
                        _placeButtonForLeftPanel();

                        // MW: Moved inside != null check 
                        if (State == JASidePanelState.LeftVisible)
                        {
                            VisiblePanelViewController = _leftPanelViewController;
                        }
                    }
                }
            }
        }

        public UIViewController CenterPanelViewController
        {
            get
            {
                return _centerPanelViewController;
            }
            set
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (value == null) throw new Exception("CenterPanelViewController cannot be set to null");

                UIViewController previous = _centerPanelViewController;

                // Add a key value observer to monitor changes to this Controllers View and ViewControllers
                if (value != previous)
                {
                    if (previous != null)
                    {
                        // If not first time creation, then remove observers on existing controller
                        previous.RemoveObserver(this, new NSString("view"));
                        previous.RemoveObserver(this, new NSString("viewControllers"));
                    }

                    _centerPanelViewController = value;

                    _centerPanelViewController.AddObserver(this, new NSString("viewControllers"), 0, ja_kvoContext);
                    _centerPanelViewController.AddObserver(this, new NSString("view"), NSKeyValueObservingOptions.Initial, ja_kvoContext);

                    if (State == JASidePanelState.CenterVisible)
                    {
                        VisiblePanelViewController = _centerPanelViewController;
                    }
                }

                if (IsViewLoaded && State == JASidePanelState.CenterVisible)
                {
                    _swapCenterWith(previous, _centerPanelViewController);
                }
                else if (IsViewLoaded)
                {
                    JASidePanelState previousState = State;
                    State = JASidePanelState.CenterVisible;
                    UIView.Animate(0.2, () =>
                    {
                        if (BounceOnCenterPanelChange)
                        {
                            float x = (previousState == JASidePanelState.LeftVisible) ? View.Bounds.Size.Width : -View.Bounds.Size.Width;
                            _centerPanelRestingFrame = new RectangleF(x, _centerPanelRestingFrame.Location.Y, _centerPanelRestingFrame.Size.Width, _centerPanelRestingFrame.Size.Height);
                        }

                        CenterPanelContainer.Frame = _centerPanelRestingFrame;
                    }, () =>
                    {
                        _swapCenterWith(previous, _centerPanelViewController);
                        _showCenterPanel(true, false);
                    });
                }
            }
        }

        public UIViewController RightPanelViewController
        {
            get
            {
                return _rightPanelViewController;
            }
            set
            {
                ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

                if (value != _rightPanelViewController)
                {
                    if (_rightPanelViewController != null)
                    {
                        _rightPanelViewController.WillMoveToParentViewController(null);
                        _rightPanelViewController.View.RemoveFromSuperview();
                        _rightPanelViewController.RemoveFromParentViewController();
                    }

                    _rightPanelViewController = value;

                    if (_rightPanelViewController != null)
                    {
                        AddChildViewController(_rightPanelViewController);
                        _rightPanelViewController.DidMoveToParentViewController(this);

                        if (State == JASidePanelState.RightVisible)
                        {
                            VisiblePanelViewController = _rightPanelViewController;
                        }
                    }
                }
            }
        }

        public float LeftGapPercentage { get; set; }
        public float LeftFixedWidth { get; set; }
        public float RightGapPercentage { get; set; }
        public float RightFixedWidth { get; set; }
        public float MinimumMovePercentage { get; set; }
        public float MaximumAnimationDuration { get; set; }
        public float BounceDuration { get; set; }
        public float BouncePercentage { get; set; }
        public bool PanningLimitedToTopViewController { get; set; }
        public bool RecognizesPanGesture { get; set; }
        public bool CanUnloadRightPanel { get; set; }
        public bool CanUnloadLeftPanel { get; set; }
        public bool ShouldResizeLeftPanel { get; set; } // Should you resize your left panel to fit in space left by gap (rather than leaving centre window overlapping after show)
        public bool ShouldResizeRightPanel { get; set; }
        public bool AllowLeftOverpan { get; set; }
        public bool AllowRightOverpan { get; set; }
        public bool BounceOnSidePanelOpen { get; set; }
        public bool BounceOnSidePanelClose { get; set; }
        public bool BounceOnCenterPanelChange { get; set; }
        public bool ShouldDelegateAutorotateToVisiblePanel { get; set; }
        public bool AllowLeftSwipe { get; set; }
        public bool AllowRightSwipe { get; set; }

        public bool CenterPanelHidden
        {
            get
            {
                return _centerPanelHidden;
            }
            set
            {
                SetCenterPanel(value, false, 0.0);
            }
        }

        public UIImage DefaultImage()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            // The image was intended to only be initialised once using dispatch_once for thread safety.
            // Could do implement of http://msdn.microsoft.com/en-gb/library/ff650316.aspx if required
            if (_defaultImage == null)
            {
                UIGraphics.BeginImageContextWithOptions(new SizeF(20f, 13f), false, 0.0f); // see this as required http://tirania.org/blog/archive/2010/Jul-20-2.html
                UIColor.Black.SetFill();
                UIBezierPath.FromRect(new RectangleF(0, 0, 20, 1)).Fill();
                UIBezierPath.FromRect(new RectangleF(0, 5, 20, 1)).Fill();
                UIBezierPath.FromRect(new RectangleF(0, 10, 20, 1)).Fill();
                UIColor.White.SetFill();
                UIBezierPath.FromRect(new RectangleF(0, 1, 20, 2)).Fill();
                UIBezierPath.FromRect(new RectangleF(0, 6, 20, 2)).Fill();
                UIBezierPath.FromRect(new RectangleF(0, 11, 20, 2)).Fill();
                _defaultImage = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
            };

            return _defaultImage;
        }

        void Dealloc()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (_centerPanelViewController != null)
            {
                _centerPanelViewController.RemoveObserver(this, new NSString("view"));
                _centerPanelViewController.RemoveObserver(this, new NSString("viewControllers"));
            }
        }

        public JASidePanelController(IntPtr ptr)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _baseInit();
        }

        public JASidePanelController()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _baseInit();
        }

        void _baseInit()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            Style = JASidePanelStyle.SingleActive;
            LeftFixedWidth = 0;
            RightFixedWidth = 0;
            LeftGapPercentage = 0.8f;
            RightGapPercentage = 0.8f;
            MinimumMovePercentage = 0.15f;
            MaximumAnimationDuration = 0.2f;
            BounceDuration = 0.1f;
            BouncePercentage = 0.075f;
            PanningLimitedToTopViewController = true;
            RecognizesPanGesture = true;
            AllowLeftOverpan = true;
            AllowRightOverpan = true;
            BounceOnSidePanelOpen = true;
            BounceOnSidePanelClose = false;
            BounceOnCenterPanelChange = true;
            ShouldDelegateAutorotateToVisiblePanel = true;
            ShouldResizeLeftPanel = false;
            ShouldResizeRightPanel = false;
            AllowRightSwipe = true;
            AllowLeftSwipe = true;
            CustomLeftButtonBarImage = null;

            DebugOutput = false;
        }

        public override void ViewDidLoad()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            base.ViewDidLoad();
            View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
            CenterPanelContainer = new UIView(View.Bounds);

            _centerPanelRestingFrame = CenterPanelContainer.Frame;
            _centerPanelHidden = false;
            LeftPanelContainer = new UIView(View.Bounds);
            LeftPanelContainer.Hidden = true;
            RightPanelContainer = new UIView(View.Bounds);
            RightPanelContainer.Hidden = true;
            _configureContainers();
            View.AddSubview(CenterPanelContainer);
            View.AddSubview(LeftPanelContainer);
            View.AddSubview(RightPanelContainer);
            State = JASidePanelState.CenterVisible;
            _swapCenterWith(null, _centerPanelViewController);
            View.BringSubviewToFront(CenterPanelContainer);

            ConsoleAllFrames();
        }

        public override void ViewWillAppear(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            base.ViewWillAppear(animated);
            _layoutSideContainersDuration(false, 0.0f);
            _layoutSidePanels();
            CenterPanelContainer.Frame = _adjustCenterFrame();
            StyleContainer(CenterPanelContainer, false, 0.0f);
        }

        public override void ViewDidAppear(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            base.ViewDidAppear(animated);
            _adjustCenterFrame();
        }

#if (!IPHONE_6_0) // Do we need any more control than this as per original version?
        public override void ViewDidUnload()
        {
            ConsoleWriteLine("<IPhone6" + MethodBase.GetCurrentMethod().Name);

            base.ViewDidUnload();
            TapView = null;
            CenterPanelContainer = null;
            LeftPanelContainer = null;
            RightPanelContainer = null;
        }

        public override bool ShouldAutorotateToInterfaceOrientation(UIInterfaceOrientation toInterfaceOrientation)
        {
            ConsoleWriteLine("<IPhone6: " + MethodBase.GetCurrentMethod().Name);

            UIViewController visiblePanel = VisiblePanelViewController;

            if (visiblePanel != null && ShouldDelegateAutorotateToVisiblePanel)
            {
                return visiblePanel.ShouldAutorotateToInterfaceOrientation(toInterfaceOrientation);
            }
            else
            {
                return true;
            }

        }
#else
        public override bool ShouldAutorotate()
        {
			ConsoleWriteLine(">=IPhone6" + MethodBase.GetCurrentMethod().Name);

            UIViewController visiblePanel = this.VisiblePanel;
			
            if (this.ShouldDelegateAutorotateToVisiblePanel && visiblePanel.RespondsToSelector(new MonoTouch.ObjCRuntime.Selector("ShouldAutorotate")))
            {
                return visiblePanel.ShouldAutorotate();
            }
            else
            {
                return true;
            }

        }
#endif

        public override void WillAnimateRotation(UIInterfaceOrientation toInterfaceOrientation, double duration)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            CenterPanelContainer.Frame = _adjustCenterFrame();
            _layoutSideContainersDuration(true, duration);
            _layoutSidePanels();
            StyleContainer(CenterPanelContainer, true, duration);
            if (CenterPanelHidden)
            {
                RectangleF frame = CenterPanelContainer.Frame;
                frame.Location = new PointF(State == JASidePanelState.LeftVisible ? CenterPanelContainer.Frame.Size.Width : -CenterPanelContainer.Frame.Size.Width, frame.Location.Y);
                CenterPanelContainer.Frame = frame;
            }
        }

        public void StyleContainer(UIView container, bool animate, TimeInterval duration)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            UIBezierPath shadowPath = UIBezierPath.FromRoundedRect(container.Bounds, 0.0f);
            if (animate)
            {
                CABasicAnimation animation = CABasicAnimation.FromKeyPath("shadowPath");
                animation.From = FromObject(container.Layer.ShadowPath);
                animation.To = FromObject(shadowPath.CGPath);
                animation.Duration = duration;
                container.Layer.AddAnimation(animation, "shadowPath");
            }

            container.Layer.ShadowPath = shadowPath.CGPath;
            container.Layer.ShadowColor = UIColor.Black.CGColor;
            container.Layer.ShadowRadius = 10.0f;
            container.Layer.ShadowOpacity = 0.75f;
            container.ClipsToBounds = false;
        }

        public void StylePanel(UIView panel)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            panel.Layer.CornerRadius = 6.0f;
            panel.ClipsToBounds = true;
        }

        /// <summary>
        /// Set All Containers to UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin and CenterPanelContainer.Frame = this.View.Bounds
        /// </summary>
        void _configureContainers()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            LeftPanelContainer.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleRightMargin;
            RightPanelContainer.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleHeight;
            CenterPanelContainer.Frame = View.Bounds;
            CenterPanelContainer.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        }

        void _layoutSideContainersDuration(bool animate, TimeInterval duration)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            RectangleF leftFrame = View.Bounds;
            RectangleF rightFrame = View.Bounds;
            if (Style == JASidePanelStyle.MultipleActive)
            {
                leftFrame.Size = new SizeF(LeftVisibleWidth, leftFrame.Size.Height);
                leftFrame.Location = new PointF(CenterPanelContainer.Frame.Location.X - leftFrame.Size.Width, leftFrame.Location.Y);
                rightFrame.Size = new SizeF(RightVisibleWidth, rightFrame.Size.Height);
                rightFrame.Location = new PointF(CenterPanelContainer.Frame.Location.X + CenterPanelContainer.Frame.Size.Width, rightFrame.Location.Y);
            }

            LeftPanelContainer.Frame = leftFrame;
            RightPanelContainer.Frame = rightFrame;
            StyleContainer(LeftPanelContainer, animate, duration);
            StyleContainer(RightPanelContainer, animate, duration);
        }

        void _layoutSidePanels()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (RightPanelViewController != null && RightPanelViewController.IsViewLoaded)
            {
                RectangleF frame = RightPanelContainer.Bounds;
                if (ShouldResizeRightPanel)
                {
                    frame.Location = new PointF(RightPanelContainer.Bounds.Size.Width - RightVisibleWidth, frame.Location.Y);
                    frame.Size = new SizeF(RightVisibleWidth, frame.Size.Height);
                }

                RightPanelViewController.View.Frame = frame;
            }

            if (LeftPanelViewController != null && LeftPanelViewController.IsViewLoaded)
            {
                RectangleF frame = LeftPanelContainer.Bounds;
                ConsolePrintViewFrame("_layoutSidePanels:this.LeftPanelContainer.Bounds", frame);
                if (ShouldResizeLeftPanel)
                {
                    frame.Size = new SizeF(LeftVisibleWidth, frame.Size.Height);
                }

                ConsolePrintViewFrame("_layoutSidePanels:after ShouldResizeLeftPanel", frame);
                LeftPanelViewController.View.Frame = frame;
            }
        }

        void _swapCenterWith(UIViewController previous, UIViewController next)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (previous != next)
            {
                if (previous != null)
                {
                    previous.WillMoveToParentViewController(null);
                    previous.View.RemoveFromSuperview();
                    previous.RemoveFromParentViewController();
                }

                if (next != null)
                {
                    _loadCenterPanel();
                    AddChildViewController(next);
                    CenterPanelContainer.AddSubview(next.View);
                    next.DidMoveToParentViewController(this);
                }
            }
        }

        void _placeButtonForLeftPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (LeftPanelViewController != null)
            {
                if (CenterPanelViewController == null) throw new ArgumentException("Please set .CenterPanel = YourViewController before instantiating Left Panel");
                UIViewController buttonController = CenterPanelViewController;

                // http://stackoverflow.com/questions/184681/is-vs-typeof
                if (buttonController.GetType() == typeof(UINavigationController))
                {
                    UINavigationController nav = (UINavigationController)buttonController;
                    if (nav.ViewControllers.Length > 0)
                    {
                        buttonController = nav.ViewControllers[0];
                    }

                }

                if (buttonController.NavigationItem.LeftBarButtonItem == null)
                {
                    buttonController.NavigationItem.SetLeftBarButtonItem(LeftButtonForCenterPanel(), false);
                }
            }
        }

        bool GestureRecognizerShouldBegin(UIGestureRecognizer gestureRecognizer)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (gestureRecognizer.View == TapView)
            {
                return true;
            }
            else if (PanningLimitedToTopViewController && !_isOnTopLevelViewController(CenterPanelViewController))
            {
                return false;
            }
            else if (gestureRecognizer.GetType() == typeof(UIPanGestureRecognizer))
            {
                UIPanGestureRecognizer pan = (UIPanGestureRecognizer)gestureRecognizer;
                PointF translate = pan.TranslationInView(CenterPanelContainer);
                if (translate.X < 0 && !AllowRightSwipe)
                {
                    return false;
                }

                if (translate.X > 0 && !AllowLeftSwipe)
                {
                    return false;
                }

                bool possible = translate.X != 0 && (((float)Math.Abs(translate.Y) / (float)Math.Abs(translate.X)) < 1.0f);
                if (possible && ((translate.X > 0 && (LeftPanelViewController != null)) || (translate.X < 0 && (RightPanelViewController != null))))
                {
                    return true;
                }

            }

            return false;
        }

        void _addPanGestureToView(UIView view)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            UIPanGestureRecognizer panGesture = new UIPanGestureRecognizer(this, new Selector("HandlePan"));
            panGesture.WeakDelegate = this; // We must Export our weak delegate functions (see HandlePan below)
            panGesture.MaximumNumberOfTouches = 1;
            panGesture.MinimumNumberOfTouches = 1;
            view.AddGestureRecognizer(panGesture);
        }

        [Export("HandlePan")]
        public void HandlePan(UIGestureRecognizer sender)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (!RecognizesPanGesture)
            {
                return;
            }

            if (sender.GetType() == typeof(UIPanGestureRecognizer))
            {
                UIPanGestureRecognizer pan = (UIPanGestureRecognizer)sender;
                if (pan.State == UIGestureRecognizerState.Began)
                {
                    _locationBeforePan = CenterPanelContainer.Frame.Location;
                }

                PointF translate = pan.TranslationInView(CenterPanelContainer);
                RectangleF frame = _centerPanelRestingFrame;
                frame.Location = new PointF(frame.Location.X + _correctMovement(translate.X), frame.Location.Y);
                CenterPanelContainer.Frame = frame;
                if (State == JASidePanelState.CenterVisible)
                {
                    if (frame.Location.X > 0.0f)
                    {
                        _loadLeftPanel();
                    }
                    else if (frame.Location.X < 0.0f)
                    {
                        _loadRightPanel();
                    }

                }

                if (sender.State == UIGestureRecognizerState.Ended)
                {
                    float deltaX = frame.Location.X - _locationBeforePan.X;
                    if (_validateThreshold(deltaX))
                    {
                        _completePan(deltaX);
                    }
                    else
                    {
                        _undoPan();
                    }

                }
                else if (sender.State == UIGestureRecognizerState.Cancelled)
                {
                    _undoPan();
                }

            }

        }

        void _completePan(float deltaX)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            switch (State)
            {
                case JASidePanelState.CenterVisible:
                    {
                        if (deltaX > 0.0f)
                        {
                            _showLeftPanel(true, BounceOnSidePanelOpen);
                        }
                        else
                        {
                            _showRightPanelBounce(true, BounceOnSidePanelOpen);
                        }

                        break;
                    }
                case JASidePanelState.LeftVisible:
                    {
                        _showCenterPanel(true, BounceOnSidePanelClose);
                        break;
                    }
                case JASidePanelState.RightVisible:
                    {
                        _showCenterPanel(true, BounceOnSidePanelClose);
                        break;
                    }
            }

        }

        void _undoPan()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            switch (State)
            {
                case JASidePanelState.CenterVisible:

                    _showCenterPanel(true, false);
                    break;

                case JASidePanelState.LeftVisible:

                    _showLeftPanel(true, false);
                    break;

                case JASidePanelState.RightVisible:

                    _showRightPanelBounce(true, false);
                    break;

            }

        }

        void _addTapGestureToView(UIView view)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            UITapGestureRecognizer tapGesture = new UITapGestureRecognizer(this, new Selector("CenterPanelTapped"));
            view.AddGestureRecognizer(tapGesture);
        }

        [Export("CenterPanelTapped")]
        public void CenterPanelTapped(UIGestureRecognizer gesture)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _showCenterPanel(true, false);
        }

        float _correctMovement(float movement)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            float position = _centerPanelRestingFrame.Location.X + movement;
            if (State == JASidePanelState.CenterVisible)
            {
                if ((position > 0.0f && (LeftPanelViewController == null)) || (position < 0.0f && (RightPanelViewController == null)))
                {
                    return 0.0f;
                }
                else if (!AllowLeftOverpan && position > LeftVisibleWidth)
                {
                    return LeftVisibleWidth;
                }
                else if (!AllowRightOverpan && position < -RightVisibleWidth)
                {
                    return -RightVisibleWidth;
                }

            }
            else if (State == JASidePanelState.RightVisible && !AllowRightOverpan)
            {
                if (position < -RightVisibleWidth)
                {
                    return 0.0f;
                }
                else if (position > RightPanelContainer.Frame.Location.X)
                {
                    return RightPanelContainer.Frame.Location.X - _centerPanelRestingFrame.Location.X;
                }

            }
            else if (State == JASidePanelState.LeftVisible && !AllowLeftOverpan)
            {
                if (position > LeftVisibleWidth)
                {
                    return 0.0f;
                }
                else if (position < LeftPanelContainer.Frame.Location.X)
                {
                    return LeftPanelContainer.Frame.Location.X - _centerPanelRestingFrame.Location.X;
                }

            }

            return movement;
        }

        bool _validateThreshold(float movement)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            float minimum = (float)Math.Floor(View.Bounds.Size.Width * MinimumMovePercentage);
            switch (State)
            {
                case JASidePanelState.LeftVisible:
                    {
                        return movement <= -minimum;
                    }
                case JASidePanelState.CenterVisible:
                    {
                        return (float)Math.Abs(movement) >= minimum;
                    }
                case JASidePanelState.RightVisible:
                    {
                        return movement >= minimum;
                    }
            }

            return false;
        }

        bool _isOnTopLevelViewController(UIViewController root)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (root.GetType() == typeof(UINavigationController))
            {
                UINavigationController nav = (UINavigationController)root;
                return nav.ViewControllers.Length == 1;
            }
            else if (root.GetType() == typeof(UITabBarController))
            {
                UITabBarController tab = (UITabBarController)root;
                return _isOnTopLevelViewController(tab.SelectedViewController);
            }

            return root != null;
        }

        void _loadCenterPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _placeButtonForLeftPanel();
            _centerPanelViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            _centerPanelViewController.View.Frame = CenterPanelContainer.Bounds;
            StylePanel(_centerPanelViewController.View);
        }

        void _loadLeftPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            RightPanelContainer.Hidden = true;
            if (LeftPanelContainer.Hidden && (LeftPanelViewController != null))
            {
                if (_leftPanelViewController.View.Superview == null)
                {
                    _layoutSidePanels();
                    _leftPanelViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    StylePanel(_leftPanelViewController.View);
                    LeftPanelContainer.AddSubview(_leftPanelViewController.View);
                }

                LeftPanelContainer.Hidden = false;
            }

        }

        void _loadRightPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            LeftPanelContainer.Hidden = true;
            if (RightPanelContainer.Hidden && (RightPanelViewController != null))
            {
                if (_rightPanelViewController.View.Superview == null)
                {
                    _layoutSidePanels();
                    _rightPanelViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                    StylePanel(_rightPanelViewController.View);
                    RightPanelContainer.AddSubview(_rightPanelViewController.View);
                }

                RightPanelContainer.Hidden = false;
            }

        }

        void _unloadPanels()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (CanUnloadLeftPanel && LeftPanelViewController.IsViewLoaded)
            {
                LeftPanelViewController.View.RemoveFromSuperview();
            }

            if (CanUnloadRightPanel && RightPanelViewController.IsViewLoaded)
            {
                RightPanelViewController.View.RemoveFromSuperview();
            }

        }

        float _calculatedDuration()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            float remaining = (float)Math.Abs(CenterPanelContainer.Frame.Location.X - _centerPanelRestingFrame.Location.X);
            float max = _locationBeforePan.X == _centerPanelRestingFrame.Location.X ? remaining : (float)Math.Abs(_locationBeforePan.X - _centerPanelRestingFrame.Location.X);
            return max > 0.0f ? MaximumAnimationDuration * (remaining / max) : MaximumAnimationDuration;
        }

        void _animateCenterPanel(bool shouldBounce, UICompletionHandler completionhandler)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            float bounceDistance = (_centerPanelRestingFrame.Location.X - CenterPanelContainer.Frame.Location.X) * BouncePercentage;

            if (_centerPanelRestingFrame.Size.Width > CenterPanelContainer.Frame.Size.Width)
            {
                shouldBounce = false;
            }

            float duration = _calculatedDuration();

            UIView.Animate(duration, 0.0f, UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews,
                () =>
                {
                    CenterPanelContainer.Frame = _centerPanelRestingFrame;
                    StyleContainer(CenterPanelContainer, true, duration);

                    ConsoleAllFrames();

                    if (Style == JASidePanelStyle.MultipleActive)
                    {
                        _layoutSideContainersDuration(false, 0.0f);
                    }
                }, () =>
                {
                    if (shouldBounce)
                    {
                        if (State == JASidePanelState.CenterVisible)
                        {
                            if (bounceDistance > 0.0f)
                            {
                                _loadLeftPanel();
                            }
                            else
                            {
                                _loadRightPanel();
                            }
                        }

                        UIView.Animate(BounceDuration, 0.0f, UIViewAnimationOptions.CurveEaseOut, () =>
                        {
                            RectangleF bounceFrame = _centerPanelRestingFrame;
                            bounceFrame.Location = new PointF(bounceFrame.Location.X + bounceDistance, bounceFrame.Location.Y);
                            CenterPanelContainer.Frame = bounceFrame;
                        }, () =>
                        {
                            UIView.AnimateNotify(BounceDuration, 0.0f, UIViewAnimationOptions.CurveEaseIn,
                            () =>
                            {
                                CenterPanelContainer.Frame = _centerPanelRestingFrame;
                            }, (finished) =>
                            {
                                if (completionhandler != null) completionhandler(finished);
                            });
                        });
                    } // if (shouldBounce)
                    else if (completionhandler != null)
                    {
                        completionhandler(true);
                    }

                });
        }

        RectangleF _adjustCenterFrame()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            RectangleF frame = View.Bounds;
            switch (State)
            {
                case JASidePanelState.CenterVisible:
                    {
                        ConsoleWriteLine(MethodBase.GetCurrentMethod().Name + ":CenterVisible");

                        frame.Location = new PointF(0.0f, frame.Location.Y);
                        if (Style == JASidePanelStyle.MultipleActive)
                        {
                            frame.Size = new SizeF(View.Bounds.Size.Width, frame.Size.Height);
                        }

                        break;
                    }
                case JASidePanelState.LeftVisible:
                    {
                        frame.Location = new PointF(LeftVisibleWidth, frame.Location.Y);
                        if (Style == JASidePanelStyle.MultipleActive)
                        {
                            frame.Size = new SizeF(View.Bounds.Size.Width - LeftVisibleWidth, frame.Size.Height);
                        }

                        break;
                    }
                case JASidePanelState.RightVisible:
                    {
                        frame.Location = new PointF(-RightVisibleWidth, frame.Location.Y);
                        if (Style == JASidePanelStyle.MultipleActive)
                        {
                            frame.Location = new PointF(0.0f, frame.Location.Y);
                            frame.Size = new SizeF(View.Bounds.Size.Width - RightVisibleWidth, frame.Size.Height);
                        }

                        break;
                    }
            }

            _centerPanelRestingFrame = frame;

            return _centerPanelRestingFrame;
        }

        void _showLeftPanel(bool animated, bool shouldBounce)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            State = JASidePanelState.LeftVisible;
            _loadLeftPanel();
            _adjustCenterFrame();

            if (animated)
            {
                _animateCenterPanel(shouldBounce, null);
            }
            else
            {

                CenterPanelContainer.Frame = _centerPanelRestingFrame;
                StyleContainer(CenterPanelContainer, false, 0.0f);

                if (Style == JASidePanelStyle.MultipleActive)
                {
                    _layoutSideContainersDuration(false, 0.0f);
                }
            }

            if (Style == JASidePanelStyle.SingleActive)
            {
                TapView = new UIView();
            }

            _toggleScrollsToTopForCenter(false, true, false);
        }

        void _showRightPanelBounce(bool animated, bool shouldBounce)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            State = JASidePanelState.RightVisible;
            _loadRightPanel();
            _adjustCenterFrame();
            if (animated)
            {
                _animateCenterPanel(shouldBounce, null);
            }
            else
            {
                CenterPanelContainer.Frame = _centerPanelRestingFrame;
                StyleContainer(CenterPanelContainer, false, 0.0f);
                if (Style == JASidePanelStyle.MultipleActive)
                {
                    _layoutSideContainersDuration(false, 0.0f);
                }

            }

            if (Style == JASidePanelStyle.SingleActive)
            {
                TapView = new UIView();
            }

            _toggleScrollsToTopForCenter(false, false, true);
        }

        void _showCenterPanel(bool animated, bool shouldBounce)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            State = JASidePanelState.CenterVisible;
            _adjustCenterFrame();

            if (animated)
            {
                _animateCenterPanel(shouldBounce, (finished) =>
                {
                    LeftPanelContainer.Hidden = true;
                    RightPanelContainer.Hidden = true;
                    _unloadPanels();
                });
            }
            else
            {
                CenterPanelContainer.Frame = _centerPanelRestingFrame;
                StyleContainer(CenterPanelContainer, false, 0.0f);
                if (Style == JASidePanelStyle.MultipleActive)
                {
                    _layoutSideContainersDuration(false, 0.0f);
                }

                LeftPanelContainer.Hidden = true;
                RightPanelContainer.Hidden = true;
                _unloadPanels();
            }

            TapView = null;
            _toggleScrollsToTopForCenter(true, false, false);

        }

        void _hideCenterPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            CenterPanelContainer.Hidden = true;
            if (CenterPanelViewController.IsViewLoaded)
            {
                CenterPanelViewController.View.RemoveFromSuperview();
            }

        }

        void _unhideCenterPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            CenterPanelContainer.Hidden = false;
            if (CenterPanelViewController.View.Superview == null)
            {
                CenterPanelViewController.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                CenterPanelViewController.View.Frame = CenterPanelContainer.Bounds;
                StylePanel(CenterPanelViewController.View);
                CenterPanelContainer.AddSubview(CenterPanelViewController.View);
            }
        }

        void _toggleScrollsToTopForCenter(bool center, bool left, bool right)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                _toggleScrollsToTop(center, CenterPanelContainer);
                _toggleScrollsToTop(left, LeftPanelContainer);
                _toggleScrollsToTop(right, RightPanelContainer);
            }
        }

        /// <summary>
        /// Recursively step through all views and subviews ensuring any UIScrollViews ScrollsTop property is set to supplied enabled parameter
        /// </summary>
        /// <returns><c>true</c>, if scrolls to top for view found with a UIScrollView to scroll to the top, <c>false</c> otherwise.</returns>
        bool _toggleScrollsToTop(bool enabled, UIView view)
        {
            if (view.GetType() == typeof(UIScrollView))
            {
                UIScrollView scrollView = (UIScrollView)view;
                scrollView.ScrollsToTop = enabled;
                return true;
            }
            else
            {
                foreach (UIView subview in view.Subviews)
                {
                    if (_toggleScrollsToTop(enabled, subview))
                    {
                        return true;
                    }

                }
            }

            return false;
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (context == ja_kvoContext)
            {
                if (keyPath == "view")
                {
                    if (CenterPanelViewController.IsViewLoaded && RecognizesPanGesture)
                    {
                        _addPanGestureToView(CenterPanelViewController.View);
                    }

                }
                else if (keyPath == "viewControllers" && ofObject == CenterPanelViewController)
                {
                    _placeButtonForLeftPanel();
                }

            }
            else
            {
                base.ObserveValue(new NSString(keyPath), ofObject, change, context);
            }

        }

        #region PublicMethods
        public UIBarButtonItem LeftButtonForCenterPanel()
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (CustomLeftButtonBarImage == null)
            {
                // Create default left menu button
                return new UIBarButtonItem(DefaultImage(), UIBarButtonItemStyle.Plain, this, new Selector("ToggleLeftPanel"));
            }

            // Use custom image based button 
            UIButton customLeftUiButton = UIButton.FromType(UIButtonType.Custom);
            UIImage customNormalUiImage = CustomLeftButtonBarImage;
            UIImage customSelectedUiImage = CustomLeftButtonBarImage;
            customLeftUiButton.SetImage(customNormalUiImage, UIControlState.Normal);
            customLeftUiButton.SetImage(customSelectedUiImage, UIControlState.Selected);
            customLeftUiButton.Frame = new RectangleF(0, 0, customSelectedUiImage.Size.Width, customSelectedUiImage.Size.Height);
            customLeftUiButton.TouchUpInside += (sender, args) => ToggleLeftPanel(this);

            return new UIBarButtonItem(customLeftUiButton);
        }

        public void ShowLeftPanel(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            ShowLeftPanelAnimated(animated);
        }

        public void ShowRightPanel(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            ShowRightPanelAnimated(animated);
        }

        public void ShowCenterPanel(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            ShowCenterPanelAnimated(animated);
        }

        public void ShowLeftPanelAnimated(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _showLeftPanel(animated, false);
        }

        public void ShowRightPanelAnimated(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            _showRightPanelBounce(animated, false);
        }

        public void ShowCenterPanelAnimated(bool animated)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (_centerPanelHidden)
            {
                _centerPanelHidden = false;
                _unhideCenterPanel();
            }

            _showCenterPanel(animated, false);
        }

        [Export("ToggleLeftPanel")]
        public void ToggleLeftPanel(UIViewController sender)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (State == JASidePanelState.LeftVisible)
            {
                _showCenterPanel(true, false);
            }
            else if (State == JASidePanelState.CenterVisible)
            {
                _showLeftPanel(true, false);
            }

        }

        [Export("ToggleRightPanel")]
        public void ToggleRightPanel(UIViewController sender)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (State == JASidePanelState.RightVisible)
            {
                _showCenterPanel(true, false);
            }
            else if (State == JASidePanelState.CenterVisible)
            {
                _showRightPanelBounce(true, false);
            }

        }

        public void SetCenterPanel(bool centerPanelHidden, bool animated, TimeInterval duration)
        {
            ConsoleWriteLine(MethodBase.GetCurrentMethod().Name);

            if (centerPanelHidden != _centerPanelHidden && State != JASidePanelState.CenterVisible)
            {
                _centerPanelHidden = centerPanelHidden;
                duration = animated ? duration : 0.0f;
                if (centerPanelHidden)
                {
                    UIView.Animate(duration, () =>
                    {
                        RectangleF frame = CenterPanelContainer.Frame;
                        frame.Location = new PointF(State == JASidePanelState.LeftVisible ? CenterPanelContainer.Frame.Size.Width : -CenterPanelContainer.Frame.Size.Width, frame.Location.Y);
                        CenterPanelContainer.Frame = frame;
                        if (ShouldResizeLeftPanel || ShouldResizeRightPanel)
                        {
                            _layoutSidePanels();
                        }

                    }, () =>
                    {
                        if (_centerPanelHidden)
                        {
                            _hideCenterPanel();
                        }

                    });
                }
                else
                {
                    _unhideCenterPanel();
                    UIView.Animate(duration, () =>
                    {
                        if (State == JASidePanelState.LeftVisible)
                        {
                            ShowLeftPanelAnimated(false);
                        }
                        else
                        {
                            ShowRightPanelAnimated(false);
                        }

                        if (ShouldResizeLeftPanel || ShouldResizeRightPanel)
                        {
                            _layoutSidePanels();
                        }

                    });
                }
            }
        }
        #endregion

        public void ConsolePrintViewFrame(string str, RectangleF uiviewframe)
        {
            ConsoleWriteLine(string.Format("[{4}] L:{0},T:{1},W:{2},H:{3}", uiviewframe.Left, uiviewframe.Top, uiviewframe.Width, uiviewframe.Height, str));
        }

        public void ConsoleAllFrames()
        {
            if (LeftPanelContainer != null) ConsolePrintViewFrame("LeftPanelContainer.Frame", LeftPanelContainer.Frame);
            if (CenterPanelContainer != null) ConsolePrintViewFrame("CenterPanelContainer.Frame", CenterPanelContainer.Frame);
            if (RightPanelContainer != null) ConsolePrintViewFrame("RightPanelContainer.Frame", RightPanelContainer.Frame);
        }

        public void ConsoleWriteLine(string str)
        {
            if (DebugOutput) Console.WriteLine(str);
        }
    }
}
