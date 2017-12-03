﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;

namespace InfiniteCanvasPOC
{
    public class InfiniteCanvas : Control
    {
        private InkSynchronizer _inkSynchronizer;
        readonly InkManager _inkManager = new InkManager();
        InkCanvas _inkCanvas;
        InfiniteCanvasOne _canvasOne;
        public InfiniteCanvas()
        {
            this.DefaultStyleKey = typeof(InfiniteCanvas);
        }

        protected override void OnApplyTemplate()
        {

            _canvasOne = (InfiniteCanvasOne)GetTemplateChild("canvasOne");

            _inkCanvas = (InkCanvas)GetTemplateChild("inkCanvas");
            var enableButton = (Button)GetTemplateChild("EnableDisableButton");
            enableButton.Click += EnableButton_Click;

            _inkSynchronizer = _inkCanvas.InkPresenter.ActivateCustomDrying();

            _inkCanvas.InkPresenter.InputDeviceTypes =
        CoreInputDeviceTypes.Mouse |
        CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            // Set initial ink stroke attributes.
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Colors.Black;
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            _inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);

            _inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            base.OnApplyTemplate();
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            _inkCanvas.Visibility = _inkCanvas.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (var s in args.Strokes)
            {
                _inkManager.AddStroke(s);
            }

            _canvasOne.DrawLine(args.Strokes);
        }
    }

    public class InfiniteCanvasOne : Canvas, IInteractionTrackerOwner
    {
        private Compositor compositor;
        private CanvasDevice win2dDevice;
        private CompositionGraphicsDevice comositionGraphicsDevice;
        private SpriteVisual myDrawingVisual;
        private CompositionVirtualDrawingSurface drawingSurface;
        private CompositionSurfaceBrush surfaceBrush;
        private InteractionTracker tracker;
        private VisualInteractionSource interactionSource;
        private CompositionPropertySet animatingPropset;
        private ExpressionAnimation animateMatrix;
        private ExpressionAnimation moveSurfaceExpressionAnimation;
        private ExpressionAnimation moveSurfaceUpDownExpressionAnimation;
        private ExpressionAnimation scaleSurfaceUpDownExpressionAnimation;

        private const int TILESIZE = 250;
        Random randonGen = new Random();

        public InfiniteCanvasOne()
        {
            InitializeComposition();
            ConfigureSpriteVisual();
            ConfigureInteraction();
            startAnimation(surfaceBrush);
            Loaded += MainPage_Loaded;
            this.SizeChanged += TheSurface_SizeChanged;
            this.PointerPressed += InfiniteCanvas_PointerPressed;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
        }

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            try
            {
                interactionSource.TryRedirectForManipulation(args.CurrentPoint);
            }
            catch
            {

            }
        }

        private void InfiniteCanvas_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            try
            {
                var currentPoint = e.GetCurrentPoint(this);
                interactionSource.TryRedirectForManipulation(currentPoint);
            }
            catch
            {

            }
        }

        private void TheSurface_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            myDrawingVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            DrawTile(new Rect(0, 0, 200, 200), 0, 0);
            DrawTile(new Rect(200, 200, 200, 200), 1, 1);

            //            drawingSurface.Trim(
            //                new[] { new RectInt32
            //                { X = 0, Y = 0, Width = 50, Height = 50 }
            //}
            //                );
        }

        public void InitializeComposition()
        {
            compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            win2dDevice = CanvasDevice.GetSharedDevice();
            comositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, win2dDevice);
            myDrawingVisual = compositor.CreateSpriteVisual();
            ElementCompositionPreview.SetElementChildVisual(this, myDrawingVisual);
        }

        public void ConfigureSpriteVisual()
        {
            var size = new Windows.Graphics.SizeInt32();
            size.Height = TILESIZE * 10000;
            size.Width = TILESIZE * 10000;

            this.drawingSurface = comositionGraphicsDevice.CreateVirtualDrawingSurface(size,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);

            this.surfaceBrush = compositor.CreateSurfaceBrush(drawingSurface);
            this.surfaceBrush.Stretch = CompositionStretch.None;
            this.surfaceBrush.HorizontalAlignmentRatio = 0;
            this.surfaceBrush.VerticalAlignmentRatio = 0;
            this.surfaceBrush.TransformMatrix = Matrix3x2.CreateTranslation(20.0f, 20.0f);

            this.myDrawingVisual.Brush = surfaceBrush;
            this.surfaceBrush.Offset = new Vector2(0, 0);
        }

        public void ConfigureInteraction()
        {
            this.interactionSource = VisualInteractionSource.Create(myDrawingVisual);
            this.interactionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
            this.interactionSource.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;

            this.interactionSource.ScaleSourceMode = InteractionSourceMode.EnabledWithInertia;

            this.tracker = InteractionTracker.CreateWithOwner(this.compositor, this);
            this.tracker.InteractionSources.Add(this.interactionSource);

            this.moveSurfaceExpressionAnimation = this.compositor.CreateExpressionAnimation("-tracker.Position.X");
            this.moveSurfaceExpressionAnimation.SetReferenceParameter("tracker", this.tracker);

            this.moveSurfaceUpDownExpressionAnimation = this.compositor.CreateExpressionAnimation("-tracker.Position.Y");
            this.moveSurfaceUpDownExpressionAnimation.SetReferenceParameter("tracker", this.tracker);

            this.scaleSurfaceUpDownExpressionAnimation = this.compositor.CreateExpressionAnimation("tracker.Scale");
            this.scaleSurfaceUpDownExpressionAnimation.SetReferenceParameter("tracker", this.tracker);

            this.tracker.MinPosition = new System.Numerics.Vector3(0, 0, 0);
            //TODO: use same consts as tilemanager object
            this.tracker.MaxPosition = new System.Numerics.Vector3(TILESIZE * 10000, TILESIZE * 10000, 0);

            this.tracker.MinScale = 0.01f;
            this.tracker.MaxScale = 100.0f;
        }

        private void startAnimation(CompositionSurfaceBrush brush)
        {
            animatingPropset = compositor.CreatePropertySet();
            animatingPropset.InsertScalar("xcoord", 1.0f);
            animatingPropset.StartAnimation("xcoord", moveSurfaceExpressionAnimation);

            animatingPropset.InsertScalar("ycoord", 1.0f);
            animatingPropset.StartAnimation("ycoord", moveSurfaceUpDownExpressionAnimation);

            animatingPropset.InsertScalar("scale", 1.0f);
            animatingPropset.StartAnimation("scale", scaleSurfaceUpDownExpressionAnimation);

            animateMatrix = compositor.CreateExpressionAnimation("Matrix3x2(props.scale, 0.0, 0.0, props.scale, props.xcoord, props.ycoord)");
            animateMatrix.SetReferenceParameter("props", animatingPropset);

            brush.StartAnimation(nameof(brush.TransformMatrix), animateMatrix);
        }

        public void DrawTile(Rect rect, int tileRow, int tileColumn)
        {
            Color randomColor = Color.FromArgb((byte)255, (byte)randonGen.Next(255), (byte)randonGen.Next(255), (byte)randonGen.Next(255));
            using (CanvasDrawingSession drawingSession = CanvasComposition.CreateDrawingSession(drawingSurface, rect))
            {
                drawingSession.Clear(randomColor);
                CanvasTextFormat tf = new CanvasTextFormat() { FontSize = 72 };
                drawingSession.DrawText($"{tileColumn},{tileRow}", new Vector2(50, 50), Colors.Green, tf);
            }
        }

        public void Trim(Rect trimRect)
        {
            drawingSurface.Trim(new RectInt32[] { new RectInt32 { X = (int)trimRect.X, Y = (int)trimRect.Y, Width = (int)trimRect.Width, Height = (int)trimRect.Height } });
        }

        public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
        {

        }

        public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
        {

        }

        public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
        {

        }

        public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
        {

        }

        public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
        {

        }

        public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
        {

        }

        public void DrawLine(IReadOnlyList<InkStroke> inkes)
        {
            // cropping part of screen
            //using (var drawingSession = CanvasComposition.CreateDrawingSession(drawingSurface,new Rect(0,0,150,150)))
            //{
            //    drawingSession.Blend = CanvasBlend.SourceOver;
            //    drawingSession.DrawInk(inkes);
            //}

            // full screen but only record the last element as every time we draw it clear the old drawings
            using (var drawingSession = CanvasComposition.CreateDrawingSession(drawingSurface, new Rect(0, 0, ActualWidth, ActualHeight)))
            {
                drawingSession.Blend = CanvasBlend.SourceOver;
                drawingSession.DrawInk(inkes);
            }
        }
    }
}
