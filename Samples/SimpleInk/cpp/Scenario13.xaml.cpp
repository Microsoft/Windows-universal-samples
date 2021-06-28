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

#include "pch.h"
#include "Scenario13.xaml.h"
#include <WindowsNumerics.h>

#define _USE_MATH_DEFINES
#include <math.h>

using namespace SDKTemplate;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Numerics;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Core;
using namespace Windows::UI::Input::Inking;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Shapes;

Scenario13::Scenario13()
{
    InitializeComponent();
    LassoSelect = (Symbol)0xEF20;

    // Initialize the InkCanvas
    inkCanvas->InkPresenter->InputDeviceTypes = CoreInputDeviceTypes::Mouse | CoreInputDeviceTypes::Pen;

    // Handlers to clear the selection when inking or erasing is detected
    inkCanvas->InkPresenter->StrokeInput->StrokeStarted += ref new TypedEventHandler<InkStrokeInput^, PointerEventArgs^>(this, &Scenario13::StrokeInput_StrokeStarted);
    inkCanvas->InkPresenter->StrokeInput->StrokeEnded += ref new TypedEventHandler<InkStrokeInput^, PointerEventArgs^>(this, &Scenario13::InkPresenter_StrokesErased);
}

void Scenario13::StrokeInput_StrokeStarted(InkStrokeInput^ sender, PointerEventArgs^ args)
{
    ClearSelection();
    inkCanvas->InkPresenter->UnprocessedInput->PointerPressed -= pointerPressedToken;
    inkCanvas->InkPresenter->UnprocessedInput->PointerMoved -= pointerMovedToken;
    inkCanvas->InkPresenter->UnprocessedInput->PointerReleased -= pointerReleasedToken;
}

void Scenario13::InkPresenter_StrokesErased(InkStrokeInput^ sender, PointerEventArgs^ args)
{
    ClearSelection();
    inkCanvas->InkPresenter->UnprocessedInput->PointerPressed -= pointerPressedToken;
    inkCanvas->InkPresenter->UnprocessedInput->PointerMoved -= pointerMovedToken;
    inkCanvas->InkPresenter->UnprocessedInput->PointerReleased -= pointerReleasedToken;
}

void Scenario13::OnSizeChanged(Object^ sender, SizeChangedEventArgs^ e)
{
    HelperFunctions::UpdateCanvasSize(RootGrid, outputGrid, inkCanvas);
}

void Scenario13::UnprocessedInput_PointerPressed(InkUnprocessedInput^ sender, PointerEventArgs^ args)
{
    if (SelectedBoudningBoxContainsPosition(args->CurrentPoint->Position))
    {
        return;
    }

    lasso = ref new Polyline();
    lasso->Stroke = ref new SolidColorBrush(Windows::UI::Colors::Blue);
    lasso->StrokeThickness = 1;
    lasso->StrokeDashArray = ref new DoubleCollection();
    lasso->StrokeDashArray->Append(5.0);
    lasso->StrokeDashArray->Append(2.0);
    lasso->Points->Append(args->CurrentPoint->RawPosition);
    selectionCanvas->Children->Append(lasso);
    isBoundRect = true;

    // Prevent most handlers along the event route from handling the same event again.
    args->Handled = true;
}

void Scenario13::UnprocessedInput_PointerMoved(InkUnprocessedInput^ sender, PointerEventArgs^ args)
{
    if (isBoundRect)
    {
        lasso->Points->Append(args->CurrentPoint->RawPosition);
    }

    // Prevent most handlers along the event route from handling the same event again.
    args->Handled = true;
}

void Scenario13::UnprocessedInput_PointerReleased(InkUnprocessedInput^ sender, PointerEventArgs^ args)
{
    lasso->Points->Append(args->CurrentPoint->RawPosition);

    boundingRect = inkCanvas->InkPresenter->StrokeContainer->SelectWithPolyLine(lasso->Points);
    isBoundRect = false;
    DrawBoundingRect();

    // Prevent most handlers along the event route from handling the same event again.
    args->Handled = true;
}

/// <summary>
/// ManipulationStarted happens when the mouse is pressed. We use this method to store what Manipulation we want to perform.
/// If it was None, then we don’t want to do anything. We were not even in the Selected Box
/// If the cursor was a SizeAll, then we wanted to move the InkStrokes. 
/// Otherwise we have one of the Diagonal cursors so we want to do a resize.
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
void Scenario13::SelectionRectangle_ManipulationStarted(Object^ sender, Windows::UI::Xaml::Input::ManipulationStartedRoutedEventArgs^ args)
{
    if (currentManipulationType != ManipulationTypes::None)
        return;

    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();

    if (window->PointerCursor->Type == CoreCursorType::SizeAll)
    {
        currentManipulationType = ManipulationTypes::Move;
    }
    else
    {
        currentManipulationType = ManipulationTypes::Size;
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
void Scenario13::SelectionRectangle_ManipulationDelta(Object^ sender, Windows::UI::Xaml::Input::ManipulationDeltaRoutedEventArgs^ e)
{
    CompositeTransform^ transform = (CompositeTransform^)selectionRectangle->RenderTransform;

    switch (currentManipulationType)
    {
    case ManipulationTypes::Move:
        MoveSelectedInkStrokes(e->Delta.Translation);
        transform->TranslateX += e->Delta.Translation.X;
        transform->TranslateY += e->Delta.Translation.Y;

        break;

    case ManipulationTypes::Size:
        //Example Scaling factor used to determine the speed at which the box will grow/shrink.
        auto scale = std::abs(1 + (float)e->Delta.Translation.X / 100);

        //Restrict scaling to a Minimum value. this well prevent negative With amounts and app crashing.
        if (selectionRectangle->Width <= 10 && scale < 1.0)
            return;

        transform->ScaleX *= scale;
        transform->ScaleY *= scale;

        auto offset = selectionRectangle->ActualOffset;
        auto center = new Windows::Foundation::Numerics::float2(offset.x + (float)transform->TranslateX, offset.y + (float)transform->TranslateY);

        ResizeSelectedInkStrokes(scale, *center);
        break;
    }
}

void Scenario13::SelectionRectangle_ManipulationCompleted(Object^ sender, Windows::UI::Xaml::Input::ManipulationCompletedRoutedEventArgs^ args)
{
    currentManipulationType = ManipulationTypes::None;
    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();
    window->PointerCursor = ref new CoreCursor(CoreCursorType::Arrow, 0);    
}

void Scenario13::SelectionRectangle_PointerPressed(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args)
{
    if (currentManipulationType != ManipulationTypes::None)
        return;

    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();

    if (window->PointerCursor->Type == CoreCursorType::SizeAll)
    {
        currentManipulationType = ManipulationTypes::Move;
    }
    else
    {
        currentManipulationType = ManipulationTypes::Size;

        switch (resizePointerCornor)
        {
        case RectangleCorner::TopLeft:
        case RectangleCorner::BottomRight:
            window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNorthwestSoutheast, 0);
            break;
        case RectangleCorner::TopRight:
        case RectangleCorner::BottomLeft:
            window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNortheastSouthwest, 0);
            break;
        }
    }
}

void Scenario13::SelectionRectangle_PointerMoved(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args)
{
    if (currentManipulationType != ManipulationTypes::None)
        return;

    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();
    Point mousePos = args->GetCurrentPoint(selectionRectangle)->Position;

    SetPointerCursorType(mousePos);
}

void Scenario13::SelectionRectangle_PointerExited(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args)
{
    if (currentManipulationType != ManipulationTypes::None)
        return;

    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();
    window->PointerCursor = ref new CoreCursor(CoreCursorType::Arrow, 0);
}

/// <summary>
/// The method will take all Strokes from the inkCanvas that have been "Selected" as per the link statement.
/// Then we will apply a Matrix3x2 transform, based on the supplied scale and the topLeft point of the bounding box.
/// </summary>
/// <param name="scale">Scaling factor to apply to the selected points</param>
/// <param name="topLeft">the "X,Y" coordinates of the Selection Box. </param>
void Scenario13::ResizeSelectedInkStrokes(float scale, Windows::Foundation::Numerics::float2& topLeft)
{

    auto allStrokes = inkCanvas->InkPresenter->StrokeContainer->GetStrokes();

    if (allStrokes == nullptr)
        return;

    float3x2 matrixSacale = make_float3x2_scale(scale, topLeft);

    for (auto stroke : allStrokes)
    {
        if (stroke->Selected)
        {
            stroke->PointTransform *= matrixSacale;
        }
    }
}

void Scenario13::MoveSelectedInkStrokes(Point position)
{
    float3x2 matrix = make_float3x2_translation((float)position.X, (float)position.Y);

        auto allStrokes = inkCanvas->InkPresenter->StrokeContainer->GetStrokes();
        if (allStrokes == nullptr)
            return;

        for (auto stroke : allStrokes)
        {
            if (stroke->Selected)
            {
                stroke->PointTransform *= matrix;
            }
        }

}

void Scenario13::DrawBoundingRect()
{
    selectionCanvas->Children->Clear();

    if (boundingRect.Width <= 0 || boundingRect.Height <= 0)
    {
        return;
    }

    selectionRectangle = ref new Rectangle();
    selectionRectangle->Name = "SelectedBox";
    selectionRectangle->Stroke = ref new SolidColorBrush(Windows::UI::Colors::Blue);
    selectionRectangle->Fill = ref new SolidColorBrush(Windows::UI::Colors::Transparent);
    selectionRectangle->StrokeThickness = 1;
    selectionRectangle->StrokeDashArray = ref new DoubleCollection();
    selectionRectangle->ManipulationMode = Windows::UI::Xaml::Input::ManipulationModes::All;
    selectionRectangle->RenderTransform = ref new CompositeTransform();
    selectionRectangle->StrokeDashArray->Append(5.0);
    selectionRectangle->StrokeDashArray->Append(2.0);
    selectionRectangle->Width = boundingRect.Width;
    selectionRectangle->Height = boundingRect.Height;

    selectionCanvas->SetLeft(selectionRectangle, boundingRect.X);
    selectionCanvas->SetTop(selectionRectangle, boundingRect.Y);

    manipulationStartedToken = selectionRectangle->ManipulationStarted += ref new Windows::UI::Xaml::Input::ManipulationStartedEventHandler(this, &Scenario13::SelectionRectangle_ManipulationStarted);
    manipulationDeltaToken = selectionRectangle->ManipulationDelta += ref new Windows::UI::Xaml::Input::ManipulationDeltaEventHandler(this, &Scenario13::SelectionRectangle_ManipulationDelta);
    manipulationCompletedToken = selectionRectangle->ManipulationCompleted += ref new Windows::UI::Xaml::Input::ManipulationCompletedEventHandler(this, &Scenario13::SelectionRectangle_ManipulationCompleted);
    pointerPressedRectangleToken = selectionRectangle->PointerPressed += ref new Windows::UI::Xaml::Input::PointerEventHandler(this, &Scenario13::SelectionRectangle_PointerPressed);
    pointerMovedRectangleToken = selectionRectangle->PointerMoved += ref new Windows::UI::Xaml::Input::PointerEventHandler(this, &Scenario13::SelectionRectangle_PointerMoved);
    pointerExitedRectangleToken = selectionRectangle->PointerExited += ref new Windows::UI::Xaml::Input::PointerEventHandler(this, &Scenario13::SelectionRectangle_PointerExited);

    selectionCanvas->Children->Append(selectionRectangle);
}

void Scenario13::ToolButton_Lasso(Object^ sender, RoutedEventArgs^ e)
{
    // By default, pen barrel button or right mouse button is processed for inking
    // Set the configuration to instead allow processing these input on the UI thread
    inkCanvas->InkPresenter->InputProcessingConfiguration->RightDragAction = InkInputRightDragAction::LeaveUnprocessed;

    pointerPressedToken = inkCanvas->InkPresenter->UnprocessedInput->PointerPressed += ref new TypedEventHandler<InkUnprocessedInput^, PointerEventArgs^>(this, &Scenario13::UnprocessedInput_PointerPressed);
    pointerMovedToken = inkCanvas->InkPresenter->UnprocessedInput->PointerMoved += ref new TypedEventHandler<InkUnprocessedInput^, PointerEventArgs^>(this, &Scenario13::UnprocessedInput_PointerMoved);
    pointerReleasedToken = inkCanvas->InkPresenter->UnprocessedInput->PointerReleased += ref new TypedEventHandler<InkUnprocessedInput^, PointerEventArgs^>(this, &Scenario13::UnprocessedInput_PointerReleased);
}

/// <summary>
/// We want to make sure that whenever we clear the SelectionBox we also unhook all out event handlers.
/// </summary>
void Scenario13::ClearDrawnBoundingRect()
{
    if (selectionCanvas->Children->Size > 0)
    {
        selectionCanvas->Children->Clear();

        selectionRectangle->ManipulationStarted -= manipulationStartedToken;
        selectionRectangle->ManipulationCompleted -= manipulationCompletedToken;
        selectionRectangle->ManipulationDelta -= manipulationDeltaToken;
        selectionRectangle->PointerExited -= pointerExitedRectangleToken;
        selectionRectangle->PointerMoved -= pointerMovedRectangleToken;
        selectionRectangle->PointerPressed -= pointerPressedRectangleToken;

        selectionRectangle = nullptr;

        boundingRect = Rect::Empty;
    }
}

void Scenario13::ClearSelection()
{
    auto strokes = inkCanvas->InkPresenter->StrokeContainer->GetStrokes();
    for (auto stroke : strokes)
    {
        stroke->Selected = false;
    }
    ClearDrawnBoundingRect();
}

bool Scenario13::SelectedBoudningBoxContainsPosition(Windows::Foundation::Point position)
{
    auto contains = !boundingRect.IsEmpty && boundingRect.Contains(position);
    return contains;
}

/// <summary>
/// Take the current pointer position and determine what cursor you want to display.
/// If the cursor is in a corner within the distance of an OffSet then display a diagonal cursor
/// If it is not in a corner, but still inside the Bounding Box, then display the 4 arrow cursor
/// otherwise it is outside the box so reset to default.
/// </summary>
/// <param name="mousePos"></param>
void Scenario13::SetPointerCursorType(Point mousePos)
{
    Windows::UI::Core::CoreWindow^ window = Windows::UI::Core::CoreWindow::GetForCurrentThread();
    window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeAll, 0);
    float offset = 5;
    float recMinX, recMaxX, recMinY, recMaxY;
    recMinX = 0;
    recMinY = 0;

    recMaxX = (float)selectionRectangle->ActualWidth;
    recMaxY = (float)selectionRectangle->ActualHeight;

    if (mousePos.X >= recMinX - offset && mousePos.X <= recMinX + offset &&
        mousePos.Y >= recMinY - offset && mousePos.Y <= recMinY + offset)
    {
        window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNorthwestSoutheast, 0);
        resizePointerCornor = RectangleCorner::TopLeft;
        return;
    }
    else if (mousePos.X >= recMaxX - offset && mousePos.X <= recMaxX + offset &&
        mousePos.Y >= recMaxY - offset && mousePos.Y <= recMaxY + offset)
    {
        window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNorthwestSoutheast, 0);
        resizePointerCornor = RectangleCorner::BottomRight;
        return;
    }
    else if (mousePos.X >= recMinX - offset && mousePos.X <= recMinX + offset &&
        mousePos.Y >= recMaxY - offset && mousePos.Y <= recMaxY + offset)
    {
        window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNortheastSouthwest, 0);
        resizePointerCornor = RectangleCorner::BottomLeft;
        return;
    }
    else if (mousePos.X >= recMaxX - offset && mousePos.X <= recMaxX + offset &&
        mousePos.Y >= recMinY - offset && mousePos.Y <= recMinY + offset)
    {
        window->PointerCursor = ref new CoreCursor(CoreCursorType::SizeNortheastSouthwest, 0);
        resizePointerCornor = RectangleCorner::TopRight;
        return;
    }
}

