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

#pragma once

#include "Scenario13.g.h"
#include "MainPage.xaml.h"

namespace SDKTemplate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [Windows::Foundation::Metadata::WebHostHidden]
    public ref class Scenario13 sealed
    {
    internal:
        /// <summary>
        /// An enum used to help determine what Pointer symbol we should use. 
        /// We can use this enum to store which corner of the Selected Boudning Box our pointer was in at the time pointer was pressed down
        /// </summary>
        enum RectangleCorner
        {
            //We will use a Diagonal a cursor of CoreCursorType.SizeNorthwestSoutheast
            TopLeft,
            //We will use a Diagonal a cursor of CoreCursorType.SizeNortheastSouthwest
            TopRight,
            //We will use a Diagonal a cursor of CoreCursorType.SizeNortheastSouthwest
            BottomLeft,
            //We will use a Diagonal a cursor of CoreCursorType.SizeNorthwestSoutheast
            BottomRight
        };

        /// <summary>
        /// An enum used to help determine what type of Manipulation we are doing to the selected InkStrokes
        /// </summary>
        enum ManipulationTypes
        {
            //No current Manipulation started - default or base condition
            None,
            //we are planning or doing a Move manipulation
            Move,
            //we are planning or doing a Resize manipulation
            Size
        };

    public:
        Scenario13();

        property Windows::UI::Xaml::Controls::Symbol LassoSelect;

    private:

        void StrokeInput_StrokeStarted(Windows::UI::Input::Inking::InkStrokeInput^ sender, Windows::UI::Core::PointerEventArgs^ args);
        void InkPresenter_StrokesErased(Windows::UI::Input::Inking::InkStrokeInput^ sender, Windows::UI::Core::PointerEventArgs^ args);

        void OnSizeChanged(Platform::Object^ sender, Windows::UI::Xaml::SizeChangedEventArgs^ e);
        void ToolButton_Lasso(Platform::Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ e);
        void MoveSelectedInkStrokes(Windows::Foundation::Point position);
        void ResizeSelectedInkStrokes(float scale, Windows::Foundation::Numerics::float2& topLeft);
        void DrawBoundingRect();
        void ClearDrawnBoundingRect();
        void ClearSelection();
        void SetPointerCursorType(Windows::Foundation::Point mousePos);

        void UnprocessedInput_PointerPressed(Windows::UI::Input::Inking::InkUnprocessedInput^ sender, Windows::UI::Core::PointerEventArgs^ args);
        void UnprocessedInput_PointerMoved(Windows::UI::Input::Inking::InkUnprocessedInput^ sender, Windows::UI::Core::PointerEventArgs^ args);
        void UnprocessedInput_PointerReleased(Windows::UI::Input::Inking::InkUnprocessedInput^ sender, Windows::UI::Core::PointerEventArgs^ args);

        void SelectionRectangle_ManipulationStarted(Object^ sender, Windows::UI::Xaml::Input::ManipulationStartedRoutedEventArgs^ args);
        void SelectionRectangle_ManipulationDelta(Object^ sender, Windows::UI::Xaml::Input::ManipulationDeltaRoutedEventArgs^ args);
        void SelectionRectangle_ManipulationCompleted(Object^ sender, Windows::UI::Xaml::Input::ManipulationCompletedRoutedEventArgs ^ args);

        void SelectionRectangle_PointerPressed(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args);
        void SelectionRectangle_PointerMoved(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args);
        void SelectionRectangle_PointerExited(Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ args);
     
        bool SelectedBoudningBoxContainsPosition(Windows::Foundation::Point position);
        
        MainPage^ rootPage = MainPage::Current;
        Windows::UI::Xaml::Shapes::Polyline^ lasso;
        Windows::Foundation::Rect boundingRect;
        Windows::UI::Xaml::Shapes::Rectangle^ selectionRectangle;
        ManipulationTypes currentManipulationType;
        RectangleCorner resizePointerCornor;
        bool isBoundRect;

        Windows::Foundation::EventRegistrationToken pointerPressedToken;
        Windows::Foundation::EventRegistrationToken pointerMovedToken;
        Windows::Foundation::EventRegistrationToken pointerReleasedToken;

        Windows::Foundation::EventRegistrationToken manipulationStartedToken;
        Windows::Foundation::EventRegistrationToken manipulationCompletedToken;
        Windows::Foundation::EventRegistrationToken manipulationDeltaToken;
        Windows::Foundation::EventRegistrationToken pointerExitedRectangleToken;
        Windows::Foundation::EventRegistrationToken pointerMovedRectangleToken;
        Windows::Foundation::EventRegistrationToken pointerPressedRectangleToken;
   

    };
}
