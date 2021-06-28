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

(function () {
    "use strict";

    var ApplicationData = Windows.Storage.ApplicationData;
    var CameraRotationHelper = SDKSample.CameraRotationHelper;
    var Capture = Windows.Media.Capture;
    var DeviceInformation = Windows.Devices.Enumeration.DeviceInformation;
    var DeviceClass = Windows.Devices.Enumeration.DeviceClass;
    var Display = Windows.Graphics.Display;
    var DisplayOrientations = Windows.Graphics.Display.DisplayOrientations;
    var KnownLibraryId = Windows.Storage.KnownLibraryId;
    var Media = Windows.Media;
    var StorageLibrary = Windows.Storage.StorageLibrary;

    // Rotation Helper to simplify handling rotation compensation for the camera streams
    var oRotationHelper = null;

    // Prevent the screen from sleeping while the camera is running
    var oDisplayRequest = new Windows.System.Display.DisplayRequest();

    // For listening to media property changes
    var oSystemMediaControls = Media.SystemMediaTransportControls.getForCurrentView();

    // MediaCapture and its state variables
    var oMediaCapture = null,
        isInitialized = false,
        isPreviewing = false,
        isRecording = false;

    // UI state
    var _isSuspending = false;
    var _isUIActive = false;
    var _setupPromise = WinJS.Promise.wrap();

    // Information about the camera device
    var externalCamera = false,
        mirroringPreview = false;
    
    // Rotation metadata to apply to the preview stream and recorded videos (MF_MT_VIDEO_ROTATION)
    // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
    var RotationKey = "C380465D-2271-428C-9B83-ECEA3B4A85C1";

    // Folder in which the captures will be stored (initialized in SetupUiAsync)
    var oCaptureFolder;

    // Initialization
    var app = WinJS.Application;
    var activation = Windows.ApplicationModel.Activation;
    app.onactivated = function (args) {
        if (args.detail.kind === activation.ActivationKind.launch) {
            if (args.detail.previousExecutionState !== activation.ApplicationExecutionState.terminated) {
                document.getElementById("photoButton").addEventListener("click", photoButton_tapped);
                document.getElementById("videoButton").addEventListener("click", videoButton_tapped);
            }

            args.setPromise(WinJS.Promise.join(setUpBasedOnStateAsync(), WinJS.UI.processAll()));
        }
    };
    
    // About to be suspended
    app.oncheckpoint = function (args) {
        _isSuspending = true;
        args.setPromise(setUpBasedOnStateAsync());
    };

    // Resuming from a user suspension
    Windows.UI.WebUI.WebUIApplication.addEventListener("resuming", function () {
        _isSuspending = false;
        setUpBasedOnStateAsync();
    }, false);
    
    // Closing
    app.onunload = function (args) {
        document.getElementById("photoButton").removeEventListener("click", photoButton_tapped);
        document.getElementById("videoButton").removeEventListener("click", videoButton_tapped);

        _isSuspending = true;
        args.setPromise(setUpBasedOnStateAsync());
    };

    document.addEventListener("visibilitychange", function () {
        setUpBasedOnStateAsync();
    });

    /// <summary>
    /// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, starts preview and unlocks the UI
    /// </summary>
    /// <returns></returns>
    function initializeCameraAsync() {
        console.log("InitializeCameraAsync");

        // Get available devices for capturing pictures
        return findCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.back)
        .then(function (camera) {
            if (camera === null) {
                console.log("No camera device found!");
                return;
            }
            // Figure out where the camera is located
            if (!camera.enclosureLocation || camera.enclosureLocation.panel === Windows.Devices.Enumeration.Panel.unknown) {
                // No information on the location of the camera, assume it's an external camera, not integrated on the device
                externalCamera = true;
            }
            else {
                // Camera is fixed on the device
                externalCamera = false;

                // Only mirror the preview if the camera is on the front panel
                mirroringPreview = (camera.enclosureLocation.panel === Windows.Devices.Enumeration.Panel.front);
            }

            // Initialize rotationHelper
            oRotationHelper = new CameraRotationHelper(camera.enclosureLocation);
            oRotationHelper.addEventListener("orientationchanged", rotationHelper_orientationChanged);

            oMediaCapture = new Capture.MediaCapture();

            // Register for a notification when video recording has reached the maximum time and when something goes wrong
            oMediaCapture.addEventListener("recordlimitationexceeded", mediaCapture_recordLimitationExceeded);
            oMediaCapture.addEventListener("failed", mediaCapture_failed);

            var settings = new Capture.MediaCaptureInitializationSettings();
            settings.videoDeviceId = camera.id;
            settings.streamingCaptureMode = Capture.StreamingCaptureMode.audioAndVideo;

            // Initialize media capture and start the preview
            return oMediaCapture.initializeAsync(settings)
            .then(function () {
                isInitialized = true;
                // Get all available media stream properties and select the first one as default
                var allProperties = oMediaCapture.videoDeviceController.getAvailableMediaStreamProperties(Capture.MediaStreamType.videoPreview);
                startPreview(oMediaCapture, Capture.MediaStreamType.videoPreview, allProperties[0]);
            });
        }, function (error) {
            console.log(error.message);
        }).done();
    }

    /// <summary>
    /// Handles an orientation changed event
    /// </summary>
    function rotationHelper_orientationChanged(event) {
        if (event.detail.updatePreview) {
            setPreviewRotationAsync();
        }
        updateButtonOrientation();
    }

    /// <summary>
    /// Uses the current device orientation in space and page orientation on the screen to calculate the rotation
    /// transformation to apply to the controls
    /// </summary>
    function updateButtonOrientation() {
        // Rotate the buttons in the UI to match the rotation of the device
        var angle = CameraRotationHelper.convertSimpleOrientationToClockwiseDegrees(oRotationHelper.getUIOrientation());

        // Rotate the buttons in the UI to match the rotation of the device
        videoButton.style.transform = "rotate(" + angle + "deg)";
        photoButton.style.transform = "rotate(" + angle + "deg)";
    }

    /// <summary>
    /// Cleans up the camera resources (after stopping any video recording and/or preview if necessary) and unregisters from MediaCapture events
    /// </summary>
    /// <returns></returns>
    function cleanupCameraAsync() {
        console.log("cleanupCameraAsync");

        var promiseList = [];

        if (isInitialized) {
            // If a recording is in progress during cleanup, stop it to save the recording
            if (isRecording) {
                var stopRecordPromise = stopRecordingAsync();
                promiseList.push(stopRecordPromise);
            }

            if (isPreviewing) {
                // The call to stop the preview is included here for completeness, but can be
                // safely removed if a call to MediaCapture.close() is being made later,
                // as the preview will be automatically stopped at that point
                stopPreview();
            }

            isInitialized = false;
        }

        // When all our tasks complete, clean up MediaCapture
        return WinJS.Promise.join(promiseList)
        .then(function () {
            if (oMediaCapture != null) {
                oMediaCapture.removeEventListener("recordlimitationexceeded", mediaCapture_recordLimitationExceeded);
                oMediaCapture.removeEventListener("failed", mediaCapture_failed);
                oMediaCapture.close();
                oMediaCapture = null;
            }
        });
    }

    /// <summary>
    /// Sets encoding properties on a camera stream. Ensures VideoElement and preview stream are stopped before setting properties.
    /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on
    /// </summary>
    function startPreview(mediaCapture, streamType, encodingProperties) {
        // Prevent the device from sleeping while the preview is running
        oDisplayRequest.requestActive();

        // Set the preview source in the UI and mirror it if necessary
        var previewVidTag = document.getElementById("cameraPreview");
        if (mirroringPreview) {
            cameraPreview.style.transform = "scale(-1, 1)";
        }

        // Apply desired stream properties
        return mediaCapture.videoDeviceController.setMediaStreamPropertiesAsync(streamType, encodingProperties)
            .then(function () {
                // Recreate pipeline and restart the preview
                previewVidTag = document.getElementById("cameraPreview");
                var previewUrl = URL.createObjectURL(mediaCapture);
                previewVidTag.src = previewUrl;
                previewVidTag.play();

                previewVidTag.addEventListener("playing", function () {
                    isPreviewing = true;
                    updateCaptureControls();
                });
            });
    }

    /// <summary>
    /// Gets the current orientation of the UI in relation to the device (when AutoRotationPreferences cannot be honored) and applies a corrective rotation to the preview
    /// </summary>
    /// <returns></returns>
    function setPreviewRotationAsync() {
        // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
        var rotation = oRotationHelper.getCameraPreviewOrientation();
        var props = oMediaCapture.videoDeviceController.getMediaStreamProperties(Capture.MediaStreamType.videoPreview);
        props.properties.insert(RotationKey, CameraRotationHelper.convertSimpleOrientationToClockwiseDegrees(rotation));
        return oMediaCapture.setEncodingPropertiesAsync(Capture.MediaStreamType.videoPreview, props, null);
    }

    /// <summary>
    /// Stops the preview and deactivates a display request, to allow the screen to go into power saving modes
    /// </summary>
    /// <returns></returns>
    function stopPreview() {
        isPreviewing = false;

        // Cleanup the UI
        var previewVidTag = document.getElementById("cameraPreview");
        previewVidTag.pause();
        previewVidTag.src = null;

        // Allow the device screen to sleep now that the preview is stopped
        oDisplayRequest.requestRelease();
    }

    /// <summary>
    /// Takes a photo to a StorageFile and adds rotation metadata to it
    /// </summary>
    /// <returns></returns>
    function takePhotoAsync() {
        // While taking a photo, keep the video button enabled only if the camera supports simultaneously taking pictures and recording video
        videoButton.disabled = oMediaCapture.mediaCaptureSettings.concurrentRecordAndPhotoSupported;

        var Streams = Windows.Storage.Streams;
        var inputStream = new Streams.InMemoryRandomAccessStream();

        // Take the picture
        console.log("Taking photo...");
        return oMediaCapture.capturePhotoToStreamAsync(Windows.Media.MediaProperties.ImageEncodingProperties.createJpeg(), inputStream)
        .then(function () {
            return oCaptureFolder.createFileAsync("SimplePhoto.jpg", Windows.Storage.CreationCollisionOption.generateUniqueName);
        })
        .then(function (file) {
            console.log("Photo taken! Saving to " + file.path);

            // Done taking a photo, so re-enable the button
            videoButton.disabled = false;

            var photoOrientation = CameraRotationHelper.convertSimpleOrientationToPhotoOrientation(oRotationHelper.getCameraCaptureOrientation());
            return reencodeAndSavePhotoAsync(inputStream, file, photoOrientation);
        }).then(function () {
            console.log("Photo saved!");
        }, function (error) {
            console.log(error.message);
        }).done();
    }

    /// <summary>
    /// Records an MP4 video to a StorageFile and adds rotation metadata to it
    /// </summary>
    /// <returns></returns>
    function startRecordingAsync() {
        return oCaptureFolder.createFileAsync("SimpleVideo.mp4", Windows.Storage.CreationCollisionOption.generateUniqueName)
        .then(function (file) {
            // Calculate rotation angle, taking mirroring into account if necessary
            var rotationAngle = CameraRotationHelper.convertSimpleOrientationToClockwiseDegrees(oRotationHelper.getCameraCaptureOrientation());
            var encodingProfile = Windows.Media.MediaProperties.MediaEncodingProfile.createMp4(Windows.Media.MediaProperties.VideoEncodingQuality.auto);
            encodingProfile.video.properties.insert(RotationKey, rotationAngle);

            console.log("Starting recording to " + file.path);
            return oMediaCapture.startRecordToStorageFileAsync(encodingProfile, file)
            .then(function () {
                isRecording = true;
                console.log("Started recording!");
            });
        });
    }

    /// <summary>
    /// Stops recording a video
    /// </summary>
    /// <returns></returns>
    function stopRecordingAsync() {
        console.log("Stopping recording...");
        return oMediaCapture.stopRecordAsync()
        .then(function () {
            isRecording = false;
            console.log("Stopped recording!");
        });
    }

    /// <summary>
    /// Attempts to find and return a device mounted on the panel specified, and on failure to find one it will return the first device listed
    /// </summary>
    /// <param name="panel">The desired panel on which the returned device should be mounted, if available</param>
    /// <returns></returns>
    function findCameraDeviceByPanelAsync(panel) {
        var deviceInfo = null;
        // Get available devices for capturing pictures
        return DeviceInformation.findAllAsync(DeviceClass.videoCapture)
        .then(function (devices) {
            devices.forEach(function (cameraDeviceInfo) {
                if (cameraDeviceInfo.enclosureLocation != null && cameraDeviceInfo.enclosureLocation.panel === panel) {
                    deviceInfo = cameraDeviceInfo;
                    return;
                }
            });

            // Nothing matched, just return the first
            if (!deviceInfo && devices.length > 0) {
                deviceInfo = devices.getAt(0);
            }

            return deviceInfo;
        });
    }

    /// <summary>
    /// Applies the given orientation to a photo stream and saves it as a StorageFile
    /// </summary>
    /// <param name="stream">The photo stream</param>
    /// <param name="photoOrientation">The orientation metadata to apply to the photo</param>
    /// <returns></returns>
    function reencodeAndSavePhotoAsync(inputStream, file, orientation) {
        var Imaging = Windows.Graphics.Imaging;
        var bitmapDecoder = null,
            bitmapEncoder = null,
            outputStream = null;

        return Imaging.BitmapDecoder.createAsync(inputStream)
        .then(function (decoder) {
            bitmapDecoder = decoder;
            return file.openAsync(Windows.Storage.FileAccessMode.readWrite);
        }).then(function (outStream) {
            outputStream = outStream;
            return Imaging.BitmapEncoder.createForTranscodingAsync(outputStream, bitmapDecoder);
        }).then(function (encoder) {
            bitmapEncoder = encoder;
            var properties = new Imaging.BitmapPropertySet();
            properties.insert("System.Photo.Orientation", new Imaging.BitmapTypedValue(orientation, Windows.Foundation.PropertyType.uint16));
            return bitmapEncoder.bitmapProperties.setPropertiesAsync(properties)
        }).then(function() {
            return bitmapEncoder.flushAsync();
        }).then(function () {
            inputStream.close();
            outputStream.close();
        });
    }

    /// <summary>
    /// This method will update the icons, enable/disable and show/hide the photo/video buttons depending on the current state of the app and the capabilities of the device
    /// </summary>
    function updateCaptureControls() {
        // The buttons should only be enabled if the preview started sucessfully
        photoButton.disabled = !isPreviewing;
        videoButton.disabled = !isPreviewing;

        // Update recording button to show "Stop" icon instead of red "Record" icon
        var vidButton = document.getElementById("videoButton").winControl;
        if (isRecording) {
            vidButton.icon = "stop";
        }
        else {
            vidButton.icon = "video";
        }

        // If the camera doesn't support simultaneously taking pictures and recording video, disable the photo button on record
        if (isInitialized && !oMediaCapture.mediaCaptureSettings.concurrentRecordAndPhotoSupported) {
            photoButton.disabled = isRecording;
        }
    }
    
    /// <summary>
    /// Attempts to lock the page orientation, hide the StatusBar (on Phone) and registers event handlers for hardware buttons and orientation sensors
    /// </summary>
    function setupUiAsync() {
        // Attempt to lock page to landscape orientation to prevent the CaptureElement from rotating, as this gives a better experience
        Display.DisplayInformation.autoRotationPreferences = DisplayOrientations.landscape;

        registerEventHandlers();

        return StorageLibrary.getLibraryAsync(KnownLibraryId.pictures)
        .then(function (picturesLibrary) {
            // Fall back to the local app storage if the Pictures Library is not available
            oCaptureFolder = picturesLibrary.saveFolder || ApplicationData.current.localFolder;

            // Hide the status bar
            if (Windows.Foundation.Metadata.ApiInformation.isTypePresent("Windows.UI.ViewManagement.StatusBar")) {
                return Windows.UI.ViewManagement.StatusBar.getForCurrentView().hideAsync();
            }
            else {
                return WinJS.Promise.as();
            }
        });
    }

    /// <summary>
    /// Unregisters event handlers for hardware buttons and orientation sensors, allows the StatusBar (on Phone) to show, and removes the page orientation lock
    /// </summary>
    /// <returns></returns>
    function cleanupUiAsync() {
        unregisterEventHandlers();

        // Show the status bar
        if (Windows.Foundation.Metadata.ApiInformation.isTypePresent("Windows.UI.ViewManagement.StatusBar")) {
            return Windows.UI.ViewManagement.StatusBar.getForCurrentView().showAsync();
        }
        else {
            return WinJS.Promise.as();
        }
    }

    /// <summary>
    /// Registers event handlers for hardware buttons and orientation sensors, and performs an initial update of the UI rotation
    /// </summary>
    function registerEventHandlers() {
        if (Windows.Foundation.Metadata.ApiInformation.isTypePresent("Windows.Phone.UI.Input.HardwareButtons")) {
            Windows.Phone.UI.Input.HardwareButtons.addEventListener("camerapressed", hardwareButtons_cameraPress);
        }

        oSystemMediaControls.addEventListener("propertychanged", systemMediaControls_PropertyChanged);
    }

    /// <summary>
    /// Unregisters event handlers for hardware buttons and orientation sensors
    /// </summary>
    function unregisterEventHandlers() {
        if (Windows.Foundation.Metadata.ApiInformation.isTypePresent("Windows.Phone.UI.Input.HardwareButtons")) {
            Windows.Phone.UI.Input.HardwareButtons.removeEventListener("camerapressed", hardwareButtons_cameraPress);
        }

        oSystemMediaControls.removeEventListener("propertychanged", systemMediaControls_PropertyChanged);
    }

    /// <summary>
    /// Initialize or clean up the camera and our UI,
    /// depending on the page state.
    /// </summary>
    /// <returns></returns>
    function setUpBasedOnStateAsync(previousPromise) {
        // Avoid reentrancy: Wait until nobody else is in this function.
        // WinJS.Promise has no way to check whether a promise has completed,
        // so we wait on the promise and then see if another task changed it.
        // if not, then it was already completed.
        if (previousPromise !== _setupPromise) {
            previousPromise = _setupPromise;
            return _setupPromise.then(function() {
                return setUpBasedOnStateAsync(previousPromise);
            });
        }

        // We want our UI to be active if
        // * We are the current active page.
        // * The window is visible.
        // * The app is not suspending.
        var wantUIActive = !document.hidden && !_isSuspending;

        if (_isUIActive != wantUIActive) {
            _isUIActive = wantUIActive;

            if (wantUIActive) {
                _setupPromise = WinJS.Promise.join(setupUiAsync(), initializeCameraAsync());
            } else {
                _setupPromise  = WinJS.Promise.join(cleanupCameraAsync(), cleanupUiAsync());
            }
        }

        return _setupPromise;
    }

    function photoButton_tapped() {
        takePhotoAsync();
    }

    function videoButton_tapped() {
        var promiseToExecute = null;
        if (!isRecording) {
            promiseToExecute = startRecordingAsync();
        }
        else {
            promiseToExecute = stopRecordingAsync();
        }
        
        promiseToExecute
        .then(function () {
            updateCaptureControls();
        }, function (error) {
            console.log(error.message);
        }).done();
    }

    /// <summary>
    /// In the event of the app being minimized this method handles media property change events. If the app receives a mute
    /// notification, it is no longer in the foregroud.
    /// </summary>
    /// <param name="args"></param>
    function systemMediaControls_PropertyChanged(args) {
        // Check to see if the app is being muted. If so, it is being minimized.
        // Otherwise if it is not initialized, it is being brought into focus.
        if (args.target.soundLevel === Media.SoundLevel.muted) {
            cleanupCameraAsync();
        }
        else if (!isInitialized) {
            initializeCameraAsync();
        }
    }

    function hardwareButtons_cameraPress() {
        takePhotoAsync();
    }

    /// <summary>
    /// This is a notification that recording has to stop, and the app is expected to finalize the recording
    /// </summary>
    function mediaCapture_recordLimitationExceeded() {
        stopRecordingAsync()
        .done(function () {
            updateCaptureControls();
        });
    }

    function mediaCapture_failed(errorEventArgs) {
        console.log("MediaCapture_Failed: 0x" + errorEventArgs.code + ": " + errorEventArgs.message);
        showErrorMessage(errorEventArgs.message);

        cleanupCameraAsync()
        .done(function() {
            updateCaptureControls();
        });    
    }
    
    /// <summary>
    /// Show error message when media capture failed
    /// Click on try again button will re-initialize and have another try
    /// </summary>
    function showErrorMessage(errormessage) {
        var msg = new Windows.UI.Popups.MessageDialog(
            "Looks like your camera is being used by another app! If you need it, here's the error message: "
            + errormessage);
        msg.commands.append(new Windows.UI.Popups.UICommand(
            "Try again",
            function () {
                initializeCameraAsync();
            })
        );
        msg.showAsync();
    }

    app.start();
})();
