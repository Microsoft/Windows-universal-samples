//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace SDKTemplate
{

    /// <summary>
    /// An enum used to help determine what Pointer symbol we should use. 
    /// We can use this enum to store which corner of the Selected Boudning Box our pointer was in at the time pointer was pressed down
    /// </summary>
    internal enum RectangleCorner
    {
        //We will use a Diagonal a cursor of CoreCursorType.SizeNorthwestSoutheast
        TopLeft,
        //We will use a Diagonal a cursor of CoreCursorType.SizeNortheastSouthwest
        TopRight,
        //We will use a Diagonal a cursor of CoreCursorType.SizeNortheastSouthwest
        BottomLeft,
        //We will use a Diagonal a cursor of CoreCursorType.SizeNorthwestSoutheast
        BottomRight
    }

    /// <summary>
    /// An enum used to help determine what type of Manipulation we are doing to the selected InkStrokes
    /// </summary>
    internal enum ManipulationTypes
    {
        //No current Manipulation started - default or base condition
        None,
        //we are planning or doing a Move manipulation
        Move,
        //we are planning or doing a Resize manipulation
        Size
    }

    /// <summary>
    /// This page shows the code to configure the InkToolbar.
    /// </summary>
    public sealed partial class Scenario13 : Page
    {
        private Polyline lasso;
        private Rect boundingRect;
        private bool isBoundRect;

        Symbol LassoSelect = (Symbol)0xEF20;
        private Rectangle selectionRectangle;
        private ManipulationTypes currentManipulationType;
        private RectangleCorner resizePointerCornor;

        public Scenario13()
        {
            this.InitializeComponent();

            // Initialize the InkCanvas
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Registering handlers to clear the selection when inking or erasing is detected
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // Unregistering handlers to clear the selection when inking or erasing is detected
            inkCanvas.InkPresenter.StrokeInput.StrokeStarted -= StrokeInput_StrokeStarted;
            inkCanvas.InkPresenter.StrokesErased -= InkPresenter_StrokesErased;
            ClearSelection();
        }

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            ClearSelection();
            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            HelperFunctions.UpdateCanvasSize(RootGrid, outputGrid, inkCanvas);
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (SelectedBoudningBoxContainsPosition(args.CurrentPoint.Position))
            {
                return;
            }

            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);
            selectionCanvas.Children.Add(lasso);
            isBoundRect = true;

            // Prevent most handlers along the event route from handling the same event again.
            args.Handled = true;
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (isBoundRect)
            {
                lasso.Points.Add(args.CurrentPoint.RawPosition);
            }

            // Prevent most handlers along the event route from handling the same event again.
            args.Handled = true;
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect = inkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(lasso.Points);
            isBoundRect = false;
            DrawBoundingRect();

            // Prevent most handlers along the event route from handling the same event again.
            args.Handled = true;
        }

        private void DrawBoundingRect()
        {
            selectionCanvas.Children.Clear();
            if (boundingRect.Width <= 0 || boundingRect.Height <= 0)
            {
                return;
            }

            selectionRectangle = new Rectangle()
            {
                Name = "SelectedBox",
                Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                Fill = new SolidColorBrush(Windows.UI.Colors.Transparent),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
                ManipulationMode = ManipulationModes.All,
                RenderTransform = new CompositeTransform(),
                Width = boundingRect.Width,
                Height = boundingRect.Height
            };

            Canvas.SetLeft(selectionRectangle, boundingRect.X);
            Canvas.SetTop(selectionRectangle, boundingRect.Y);
            
            selectionRectangle.ManipulationStarted += SelectionRectangle_ManipulationStarted;
            selectionRectangle.ManipulationDelta += SelectionRectangle_ManipulationDelta;
            selectionRectangle.ManipulationCompleted += SelectionRectangle_ManipulationCompleted;
            selectionRectangle.PointerPressed += SelectionRectangle_PointerPressed;
            selectionRectangle.PointerMoved += SelectionRectangle_PointerMoved;
            selectionRectangle.PointerExited += SelectionRectangle_PointerExited;

            selectionCanvas.Children.Add(selectionRectangle);
        }

        /// <summary>
        /// ManipulationStarted happens when the mouse is pressed. We use this method to store what Manipulation we want to perform.
        /// If it was None, then we don’t want to do anything. We were not even in the Selected Box
        /// If the cursor was a SizeAll, then we wanted to move the InkStrokes. 
        /// Otherwise we have one of the Diagonal cursors so we want to do a resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionRectangle_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (currentManipulationType != ManipulationTypes.None)
                return;

            if (Window.Current.CoreWindow.PointerCursor.Type == CoreCursorType.SizeAll)
            {
                currentManipulationType = ManipulationTypes.Move;
            }
            else
            {
                currentManipulationType = ManipulationTypes.Size;
            }
        }

        /// <summary>
        /// This is the main process for the InkStoke Manipulation. 
        /// Here we use the stored currentManipualtiaonType to determine what type of Action we want to preform (Move or Size)
        /// If its move, then we call the MoveSelectedInkStrokes method passing the pointers Delta Translation. We also apply that translation to the SelectedRectangle's Translation X and Y.
        ///     This will move both the InkStrokes and the Selected Box as the mouse pointer moves.
        /// If its Size, then we first need to calculate a Scaling Factor. We derive this based on the Pointers Delta Transform X but reducing it by 100 to help match the pointers scroll speed
        ///     The Delta X can be a Positive or Negative number, so when we add this to 1 we get a reasonable value for a Scale Factor. Typically this will fall between .90 to 1.10
        ///     we then check that this is a valid number, as scaling down can cause issues if we go too small or attempt to create Negative height/width.
        ///     and finally we apply this scale factor to the Selection Box and call the ResizeSelectedInkStrokes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionRectangle_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            var transform = (selectionRectangle.RenderTransform as CompositeTransform);

            switch (currentManipulationType)
            {
                case ManipulationTypes.Move:
                    MoveSelectedInkStrokes(e.Delta.Translation);
                    transform.TranslateX += e.Delta.Translation.X;
                    transform.TranslateY += e.Delta.Translation.Y;
                    break;

                case ManipulationTypes.Size:

                    //Example Scaling factor used to determine the speed at which the box will grow/shrink.
                    var scalingFactor = Math.Abs(1 + (float)e.Delta.Translation.X / 100);

                    //Restrict scaling to a Minimum value. this well prevent negative With amounts and app crashing.
                    if (selectionRectangle.Width <= 10 && scalingFactor < 1f)
                        return;

                    transform.ScaleX *= scalingFactor;
                    transform.ScaleY *= scalingFactor;

                    var offset = selectionRectangle.ActualOffset;

                    ResizeSelectedInkStrokes(scalingFactor, new Vector2(offset.X + (float)transform.TranslateX, offset.Y + (float)transform.TranslateY));
                    break;
            }
        }

        /// <summary>
        /// When we are done resizing/moving the InkStrokes, we reset the mouse pointer and clear the currentManipulationType placeholder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectionRectangle_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            currentManipulationType = ManipulationTypes.None;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void SelectionRectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (currentManipulationType != ManipulationTypes.None)
                return;

            if (Window.Current.CoreWindow.PointerCursor.Type == CoreCursorType.SizeAll)
            {
                currentManipulationType = ManipulationTypes.Move;
            }
            else
            {
                currentManipulationType = ManipulationTypes.Size;

                switch (resizePointerCornor)
                {
                    case RectangleCorner.TopLeft:
                    case RectangleCorner.BottomRight:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
                        break;
                    case RectangleCorner.TopRight:
                    case RectangleCorner.BottomLeft:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
                        break;
                }
            }
        }

        private void SelectionRectangle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (currentManipulationType != ManipulationTypes.None)
                return;

            var mousePos = e.GetCurrentPoint(selectionRectangle).Position;
            SetPointerCursorType(mousePos);
        }        

        private void SelectionRectangle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (currentManipulationType != ManipulationTypes.None)
                return;

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void ToolButton_Lasso(object sender, RoutedEventArgs e)
        {
            // By default, pen barrel button or right mouse button is processed for inking
            // Set the configuration to instead allow processing these input on the UI thread
            inkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            inkCanvas.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            inkCanvas.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            inkCanvas.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
        }

        /// <summary>
        /// We want to make sure that whenever we clear the SelectionBox we also unhook all out event handlers.
        /// </summary>
        private void ClearDrawnBoundingRect()
        {

            if (selectionCanvas.Children.Count > 0)
            {
                selectionCanvas.Children.Clear();
                selectionRectangle.ManipulationStarted -= SelectionRectangle_ManipulationStarted;
                selectionRectangle.ManipulationCompleted -= SelectionRectangle_ManipulationCompleted;
                selectionRectangle.ManipulationDelta -= SelectionRectangle_ManipulationDelta;
                selectionRectangle.PointerExited -= SelectionRectangle_PointerExited;
                selectionRectangle.PointerMoved -= SelectionRectangle_PointerMoved;
                selectionRectangle.PointerPressed -= SelectionRectangle_PointerPressed;

                selectionRectangle = null;
                boundingRect = Rect.Empty;
            }
        }

        private void ClearSelection()
        {
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearDrawnBoundingRect();
        }
        
        private bool SelectedBoudningBoxContainsPosition(Point position)
        {
            var contains = !boundingRect.IsEmpty && RectHelper.Contains(boundingRect, position);
            return contains;
        }

        /// <summary>
        /// The method will take all Strokes from the inkCanvas that have been "Selected" as per the link statement.
        /// Then we will apply a Matrix3x2 transform, based on the supplied scale and the topLeft point of the bounding box.
        /// </summary>
        /// <param name="scale">Scaling factor to apply to the selected points</param>
        /// <param name="topLeft">the "X,Y" coordinates of the Selection Box. </param>
        private void ResizeSelectedInkStrokes(float scale, Vector2 topLeft)
        {

            IEnumerable<InkStroke> selectedStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(x => x.Selected);
            if (selectedStrokes == null)
                return;

            Matrix3x2 matrixSacale = Matrix3x2.CreateScale(scale, topLeft);

            foreach (InkStroke inkStroke in selectedStrokes)
            {
                inkStroke.PointTransform *= matrixSacale;
            }
        }

        private void MoveSelectedInkStrokes(Point pos)
        {
            Matrix3x2 matrix = Matrix3x2.CreateTranslation((float)pos.X, (float)pos.Y);

            if (!matrix.IsIdentity)
            {
                IEnumerable<InkStroke> selectedStrokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(x => x.Selected);
                if (selectedStrokes == null)
                    return;
                foreach (InkStroke stroke in selectedStrokes)
                {
                    stroke.PointTransform *= matrix;
                }
            }
        }

        /// <summary>
        /// Take the current pointer position and determine what cursor you want to display.
        /// If the cursor is in a corner within the distance of an OffSet then display a diagonal cursor
        /// If it is not in a corner, but still inside the Bounding Box, then display the 4 arrow cursor
        /// otherwise it is outside the box so reset to default.
        /// </summary>
        /// <param name="mousePos"></param>
        private void SetPointerCursorType(Point mousePos)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
            var offset = 5;
            float recMinX, recMaxX, recMinY, recMaxY;
            recMinX = 0;
            recMinY = 0;

            recMaxX = (float)selectionRectangle.ActualWidth;
            recMaxY = (float)selectionRectangle.ActualHeight;

            if (mousePos.X >= recMinX - offset && mousePos.X <= recMinX + offset &&
                mousePos.Y >= recMinY - offset && mousePos.Y <= recMinY + offset)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
                resizePointerCornor = RectangleCorner.TopLeft;
                return;
            }
            else if (mousePos.X >= recMaxX - offset && mousePos.X <= recMaxX + offset &&
                mousePos.Y >= recMaxY - offset && mousePos.Y <= recMaxY + offset)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNorthwestSoutheast, 0);
                resizePointerCornor = RectangleCorner.BottomRight;
                return;
            }
            else if (mousePos.X >= recMinX - offset && mousePos.X <= recMinX + offset &&
                mousePos.Y >= recMaxY - offset && mousePos.Y <= recMaxY + offset)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
                resizePointerCornor = RectangleCorner.BottomLeft;
                return;
            }
            else if (mousePos.X >= recMaxX - offset && mousePos.X <= recMaxX + offset &&
                mousePos.Y >= recMinY - offset && mousePos.Y <= recMinY + offset)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeNortheastSouthwest, 0);
                resizePointerCornor = RectangleCorner.TopRight;
                return;
            }
        }
    }
}