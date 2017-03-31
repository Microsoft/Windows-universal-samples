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
//   For more information, see: http://go.microsoft.com/fwlink/?LinkID=623246
// </auto-generated>
//-----------------------------------------------------------------------------
#pragma once

namespace com { namespace microsoft { namespace Samples { namespace SecureInterface {

public interface class ISecureInterfaceService
{
public:
    // "Concatenate two input strings and returns the concatenated string as output"
    // Implement this function to handle calls to the Concatenate method.
    Windows::Foundation::IAsyncOperation<SecureInterfaceConcatenateResult^>^ ConcatenateAsync(_In_ Windows::Devices::AllJoyn::AllJoynMessageInfo^ info , _In_ Platform::String^ interfaceMemberInStr1, _In_ Platform::String^ interfaceMemberInStr2);

    // "Determine if the output of the Concatenate method is returned as upper case string or not"
    // Implement this function to handle requests for the value of the IsUpperCaseEnabled property.
    //
    // Currently, info will always be null, because no information is available about the requestor.
    Windows::Foundation::IAsyncOperation<SecureInterfaceGetIsUpperCaseEnabledResult^>^ GetIsUpperCaseEnabledAsync(Windows::Devices::AllJoyn::AllJoynMessageInfo^ info);

    // "Determine if the output of the Concatenate method is returned as upper case string or not"
    // Implement this function to handle requests to set the IsUpperCaseEnabled property.
    //
    // Currently, info will always be null, because no information is available about the requestor.
    Windows::Foundation::IAsyncOperation<SecureInterfaceSetIsUpperCaseEnabledResult^>^ SetIsUpperCaseEnabledAsync(Windows::Devices::AllJoyn::AllJoynMessageInfo^ info, bool value);

};

} } } } 
