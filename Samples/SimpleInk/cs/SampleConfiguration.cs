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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SDKTemplate
{
    public partial class MainPage : Page
    {
        public const string FEATURE_NAME = "SimpleInk sample";

        List<Scenario> scenarios = new List<Scenario>
        {
            new Scenario() { Title="Scenario 1", ClassType=typeof(Scenario1)},
            new Scenario() { Title="Scenario 2", ClassType=typeof(Scenario2)},
            new Scenario() { Title="Scenario 3", ClassType=typeof(Scenario3)},
            new Scenario() { Title="Scenario 4", ClassType=typeof(Scenario4)},
            new Scenario() { Title="Scenario 5", ClassType=typeof(Scenario5)},
            new Scenario() { Title="Scenario 6", ClassType=typeof(Scenario6)},
            new Scenario() { Title="Scenario 7", ClassType=typeof(Scenario7)},
            new Scenario() { Title="Scenario 8", ClassType=typeof(Scenario8)},
            new Scenario() { Title="Scenario 9", ClassType=typeof(Scenario9)},
            new Scenario() { Title="Scenario 10", ClassType=typeof(Scenario10)},
            new Scenario() { Title="Scenario 11", ClassType=typeof(Scenario11)},
            new Scenario() { Title="Scenario 12", ClassType=typeof(Scenario12)},
            new Scenario() { Title="Scenario 13", ClassType=typeof(Scenario13)},
        };
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }

    static class HelperFunctions
    {
        public static void UpdateCanvasSize(FrameworkElement root, FrameworkElement output, FrameworkElement inkCanvas)
        {
            output.Width = root.ActualWidth;
            output.Height = root.ActualHeight / 2;
            inkCanvas.Width = root.ActualWidth;
            inkCanvas.Height = root.ActualHeight / 2;
        }
    }

    static class MoreSymbols
    {
        static public Symbol CalligraphyPen = (Symbol)0xEDFB;
        static public Symbol LassoSelect = (Symbol)0xEF20;
        static public Symbol TouchWriting = (Symbol)0xED5F;
    }
}
