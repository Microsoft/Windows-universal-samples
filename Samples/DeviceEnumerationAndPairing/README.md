---
page_type: sample
languages:
- csharp
- cpp
- cppcx
products:
- windows
- windows-uwp
urlFragment: DeviceEnumerationAndPairing
extendedZipContent:
- path: SharedContent
  target: SharedContent
- path: LICENSE
  target: LICENSE
description: "Shows how to find and pair devices internal to the system, externally connected, or nearby over wireless or networking protocols."
---

<!---
  category: DevicesSensorsAndPower
  samplefwlink: http://go.microsoft.com/fwlink/p/?LinkId=620536
--->

# Device enumeration and pairing sample

Shows how to use the Windows.Devices.Enumeration APIs find devices internally connected to the system, externally connected, 
or nearby over wireless or networking protocols and get information about them.  It also shows how to pair wireless and networking devices with the system. 

> **Note:** This sample is part of a large collection of UWP feature samples. 
> You can download this sample as a standalone ZIP file
> [from docs.microsoft.com](https://docs.microsoft.com/samples/microsoft/windows-universal-samples/deviceenumerationandpairing/),
> or you can download the entire collection as a single
> [ZIP file](https://github.com/Microsoft/Windows-universal-samples/archive/master.zip), but be 
> sure to unzip everything to access shared dependencies. For more info on working with the ZIP file, 
> the samples collection, and GitHub, see [Get the UWP samples from GitHub](https://aka.ms/ovu2uq). 
> For more samples, see the [Samples portal](https://aka.ms/winsamples) on the Windows Dev Center. 

The sample demonstrates eight scenarios:

1.  **Device Picker Common Control:** This scenario demonstrates the use of the Windows.Devices.Enumeration.DevicePicker. DevicePicker is a UI control that allows users of your app to pick a device. After they pick a device you can get information from it and start using it through the appropriate device API. 

2.  **Enumerate and Watch Devices:** This scenario demonstrates the use of the Windows.Devices.Enumeration DeviceWatcher. Device Watcher allows you to find and receive updates for devices you're interested in. Even if you don't need updates for devices as the arrive, depart, or change, this method still has advantages over FindAllAsync in that the results come back one-by-one has they are discovered, rather than waiting for all results to be retreived and then returned in a single collection. 

3.  **Enumerate and Watch Devices in a Background Task:** This scenario demonstrates the use of the Windows.Devices.Enumeration DeviceWatcherTrigger. DeviceWatcherTrigger is similar to DeviceWatcher, but is performed in a background task.  First you create a DeviceWatcher object, but instead of starting the DeviceWatcher in the foreground, you get the background trigger and then register it. Your background task is called with initial results and then called repeatedly over time as the device you're watching arrive, depart, or change. 

4.  **Enumerate Snapshot of Devices:** This scenario demonstrates the use of the Windows.Devices.Enumeration DeviceInformation.FindAllAsync. Use for grabbing a quick snapshot of devices you're interested in. This method is simplier to use than the DeviceWatcher, but doesn't allow you to process results as they arrive.  Rather all results are internally added to a collection until the enumeration is complete, then the collection is returned when the async operation completes. 

5.  **Get Single Device:** This scenario demonstrates the use of the Windows.Devices.Enumeration DeviceInformation.CreateFromIdAsync. This would typically be used when you have a device id saved, and want to retrieve information about that device again. Other uses would be if you have a DeviceInformation.Id from another API and need to obtain a DeviceInformation object. 

6.  **Custom Filter with Additional Properties:** This scenario demonstrates using custom filters and/or requesting additional properties with the DeviceWatcher. A custom filter and/or requested additional properties can also be done with the DevicePicker, FindAllAsync, and DeviceWatcherTrigger

7.  **Request Specific DeviceInformationKind:** This scenario demonstrates requesting various DeviceInformationKinds. Methods without the DeviceInformationKind parameter default to DeviceInterface. 

8.  **Basic Device Pairing:** This scenario demonstrates how to perform basic pairing. Basic pairing allows you to tell Windows which device you want paired, and then Windows will handle the ceremony and the UI. If you want to display your own UI and be involved in the pairing ceremony, please see the Custom Pairing scenario. This method can be used to pair devices like WiFiDirect, WSD, UPnP, Bluetooth or Bluetooth LE devices. Pairing can only be performed on DeviceInformation objects of kind DeviceInformationKind.AssociationEndpoint.

9.  **Custom Device Pairing:** This scenario demonstrates how to perform custom pairing. Custom pairing allows your app to be involved in the pairing ceremony and use your on UI. If you want Windows to control the ceremony and display system UI, please look at the basic pairing scenario. This method can be used to pair devices like WiFiDirect, WSD, UPnP, Bluetooth or Bluetooth LE devices. Pairing can only be performed on DeviceInformation objects of kind DeviceInformationKind.AssociationEndpoint.


## Related topics

### Reference

[Enumerating devices article](http://msdn.microsoft.com/library/windows/apps/Hh464977)  
[Windows.Devices.Enumeration namespace](http://msdn.microsoft.com/library/windows/apps/windows.devices.enumeration.aspx)  

### Related samples

* [DeviceEnumerationAndPairing sample](/archived/DeviceEnumerationAndPairing/) for JavaScript (archived)

## System requirements

* Windows 10

## Build the sample

1.  Start Visual Studio and select **File** \> **Open** \> **Project/Solution**.
2.  Go to the directory to which you unzipped the sample. Then go to the subdirectory containing the sample in the language you desire - either C++, C\#, or JavaScript. Double-click the Visual Studio Solution (.sln) file.
3.  Select the appropriate target: x86, x64, ARM. Press Ctrl+Shift+B, or select **Build** \> **Build Solution**.


## Run the sample

The next steps depend on whether you just want to deploy the sample or you want to both deploy and run it.

### Deploying the sample

- Select **Build** \> **Deploy Solution**. 

### Deploying the sample

- To debug the sample and then run it, press F5 or select **Debug** \> **Start Debugging**. To run the sample without debugging, press Ctrl+F5 or select **Debug** \> **Start Without Debugging**.

