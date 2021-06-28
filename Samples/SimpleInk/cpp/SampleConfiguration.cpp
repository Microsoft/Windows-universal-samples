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
#include "MainPage.xaml.h"
#include "SampleConfiguration.h"

using namespace SDKTemplate;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;

Platform::Array<Scenario>^ MainPage::scenariosInner = ref new Platform::Array<Scenario>
{
    { "Scenario 1", "SDKTemplate.Scenario1" },
    { "Scenario 2", "SDKTemplate.Scenario2" },
    { "Scenario 3", "SDKTemplate.Scenario3" },
    { "Scenario 4", "SDKTemplate.Scenario4" },
    { "Scenario 5", "SDKTemplate.Scenario5" },
    { "Scenario 6", "SDKTemplate.Scenario6" },
    { "Scenario 7", "SDKTemplate.Scenario7" },
    { "Scenario 8", "SDKTemplate.Scenario8" },
    { "Scenario 9", "SDKTemplate.Scenario9" },
    { "Scenario 10", "SDKTemplate.Scenario10" },
    { "Scenario 11", "SDKTemplate.Scenario11" },
    { "Scenario 12", "SDKTemplate.Scenario12" },
    { "Scenario 13", "SDKTemplate.Scenario13" }
};

void HelperFunctions::UpdateCanvasSize(FrameworkElement^ root, FrameworkElement^ output, FrameworkElement^ inkCanvas)
{
    output->Width = root->ActualWidth;
    output->Height = root->ActualHeight / 2;
    inkCanvas->Width = root->ActualWidth;
    inkCanvas->Height = root->ActualHeight / 2;
}



