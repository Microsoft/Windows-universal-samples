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

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace SDKTemplate
{
    public sealed partial class Scenario4 : Page
    {
        private TransformGroup transforms;
        private MatrixTransform previousMatrixTransform;
        private CompositeTransform previousCompositeTransform;
        private CompositeTransform deltaTransform;
        private bool forceManipulationsToEnd;

        public Scenario4()
        {
            this.InitializeComponent();
            forceManipulationsToEnd = false;

            InitOptions();

            // Initialize the transforms that will be used to manipulate the shape
            InitManipulationTransforms();

            // Register for the various manipulation events that will occur on the shape
            manipulateMe.ManipulationStarted += new ManipulationStartedEventHandler(ManipulateMe_ManipulationStarted);
            manipulateMe.ManipulationDelta += new ManipulationDeltaEventHandler(ManipulateMe_ManipulationDelta);
            manipulateMe.ManipulationCompleted += new ManipulationCompletedEventHandler(ManipulateMe_ManipulationCompleted);
            manipulateMe.ManipulationInertiaStarting += new ManipulationInertiaStartingEventHandler(ManipulateMe_ManipulationInertiaStarting);

            // The ManipulationMode property dictates what manipulation events the element
            // will listen to.  This will set it to a limited subset of these events.
            manipulateMe.ManipulationMode =
                ManipulationModes.TranslateX |
                ManipulationModes.TranslateY |
                ManipulationModes.Rotate |
                ManipulationModes.Scale |
                ManipulationModes.ScaleInertia |
                ManipulationModes.TranslateInertia |
                ManipulationModes.RotateInertia;
        }

        private void InitManipulationTransforms()
        {
            transforms = new TransformGroup();
            
            previousMatrixTransform = new MatrixTransform() { Matrix = Matrix.Identity };
            deltaTransform = new CompositeTransform();

            transforms.Children.Add(previousMatrixTransform);
            transforms.Children.Add(deltaTransform);

            // Set the render transform on the rect
            manipulateMe.RenderTransform = transforms;
        }

        // When a manipulation begins, change the color of the object to reflect
        // that a manipulation is in progress
        void ManipulateMe_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            forceManipulationsToEnd = false;
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.DeepSkyBlue);
        }

        // Process the change resulting from a manipulation
        void ManipulateMe_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // If the reset button has been pressed, mark the manipulation as completed
            if (forceManipulationsToEnd)
            {
                e.Complete();
                return;
            }

            previousMatrixTransform.Matrix = transforms.Value;

            // Get center point for rotation
            Point center = previousMatrixTransform.TransformPoint(new Point(e.Position.X, e.Position.Y));
            deltaTransform.CenterX = center.X;
            deltaTransform.CenterY = center.Y;

            // Look at the Delta property of the ManipulationDeltaRoutedEventArgs to retrieve
            // the rotation, scale, X, and Y changes
            deltaTransform.Rotation = e.Delta.Rotation;
            deltaTransform.ScaleX = deltaTransform.ScaleY = e.Delta.Scale;
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
        }

        // When a manipulation that's a result of inertia begins, change the color of the
        // the object to reflect that inertia has taken over
        void ManipulateMe_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.RoyalBlue);
        }

        // When a manipulation has finished, reset the color of the object
        void ManipulateMe_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            manipulateMe.Background = new SolidColorBrush(Windows.UI.Colors.LightGray);
        }

        private void movementAxis_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Set the object to listen to both X and Y translation events
            manipulateMe.ManipulationMode |= ManipulationModes.TranslateX | ManipulationModes.TranslateY;

            ComboBoxItem selectedItem = (ComboBoxItem)((ComboBox)sender).SelectedItem;
            switch (selectedItem.Content.ToString())
            {
                case "X only":
                    // Set the object to not listen for translations on the Y axis
                    manipulateMe.ManipulationMode ^= ManipulationModes.TranslateY;
                    break;
                case "Y only":
                    // Set the object to not listen for translations on the X axis
                    manipulateMe.ManipulationMode ^= ManipulationModes.TranslateX;
                    break;
            }
        }

        private void InertiaSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // Flip the current TranslateInertia and RotateInertia values in response to the
            // InertiaSwitch being toggled
            if (manipulateMe != null)
            {
                manipulateMe.ManipulationMode ^= ManipulationModes.TranslateInertia | ManipulationModes.RotateInertia;
            }
        }

        //
        // More UI code below
        //
        private void InitOptions()
        {
            movementAxis.SelectedIndex = 0;
            inertiaSwitch.IsOn = true;
        }

        void resetButton_Pressed(object sender, RoutedEventArgs e)
        {
            forceManipulationsToEnd = true;
            manipulateMe.RenderTransform = null;
            movementAxis.SelectedIndex = 0;
            InitOptions();
            InitManipulationTransforms();
        }
    }
}

