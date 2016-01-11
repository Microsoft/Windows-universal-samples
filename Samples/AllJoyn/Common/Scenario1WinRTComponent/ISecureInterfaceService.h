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
//-----------------------------------------------------------------------------
// <auto-generated> 
//   This code was generated by a tool. 
// 
//   Changes to this file may cause incorrect behavior and will be lost if  
//   the code is regenerated.
//
//   Tool: AllJoynCodeGenerator.exe
//
//   This tool is located in the Windows 10 SDK and the Windows 10 AllJoyn 
//   Visual Studio Extension in the Visual Studio Gallery.  
//
//   The generated code should be packaged in a Windows 10 C++/CX Runtime  
//   Component which can be consumed in any UWP-supported language using 
//   APIs that are available in Windows.Devices.AllJoyn.
//
//   Using AllJoynCodeGenerator - Invoke the following command with a valid 
//   Introspection XML file and a writable output directory:
//     AllJoynCodeGenerator -i <INPUT XML FILE> -o <OUTPUT DIRECTORY>
// </auto-generated>
//-----------------------------------------------------------------------------
#pragma once

namespace com { namespace microsoft { namespace Samples { namespace SecureInterface {

public interface class ISecureInterfaceService
{
public:
    // Implement this function to handle calls to the Concatenate method.
    Windows::Foundation::IAsyncOperation<SecureInterfaceConcatenateResult^>^ ConcatenateAsync(_In_ Windows::Devices::AllJoyn::AllJoynMessageInfo^ info , _In_ Platform::String^ interfaceMemberInStr1, _In_ Platform::String^ interfaceMemberInStr2);

    // Implement this function to handle requests for the value of the IsUpperCaseEnabled property.
    //
    // Currently, info will always be null, because no information is available about the requestor.
    Windows::Foundation::IAsyncOperation<SecureInterfaceGetIsUpperCaseEnabledResult^>^ GetIsUpperCaseEnabledAsync(Windows::Devices::AllJoyn::AllJoynMessageInfo^ info);

    // Implement this function to handle requests to set the IsUpperCaseEnabled property.
    //
    // Currently, info will always be null, because no information is available about the requestor.
    Windows::Foundation::IAsyncOperation<SecureInterfaceSetIsUpperCaseEnabledResult^>^ SetIsUpperCaseEnabledAsync(Windows::Devices::AllJoyn::AllJoynMessageInfo^ info, bool value);

};

} } } } 
