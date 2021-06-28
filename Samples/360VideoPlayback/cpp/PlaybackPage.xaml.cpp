//
// PlaybackPage.xaml.cpp
// Implementation of the PlaybackPage class
//

#include "pch.h"
#include "PlaybackPage.xaml.h"
#include "VideoGallery.xaml.h"

using namespace _360VideoPlayback;
using namespace DirectX;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Numerics;
using namespace Windows::Gaming::Input;
using namespace Windows::Media::Core;
using namespace Windows::Media::Playback;
using namespace Windows::System;
using namespace Windows::System::Threading;
using namespace Windows::UI::Core;
using namespace Windows::UI::Popups;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;

const double THUMBSTICK_DEADZONE = 0.25f;
const int scrollStep = 2;
const int mouseWheelData = 120;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

quaternion CreateFromHeadingPitchRoll(double flHeading, double flPitch, double flRoll)
{
    // Here we are creating the quaternion assuming Tait–Bryan angles and applying the
    // angles in order of yaw, pitch, roll.
    // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
    quaternion result;

    const float headingPart = (XMConvertToRadians((float)flHeading) * 0.5f);    // clockwise
    const float sin1 = sin(headingPart);
    const float cos1 = cos(headingPart);

    const float pitchPart = (XMConvertToRadians(-(float)flPitch) * 0.5f);       // counterclockwise
    const float sin2 = sin(pitchPart);
    const float cos2 = cos(pitchPart);

    const float rollPart = (XMConvertToRadians((float)flRoll) * 0.5f);          // clockwise
    const float sin3 = sin(rollPart);
    const float cos3 = cos(rollPart);

    result.w = cos1 * cos2 * cos3 - sin1 * sin2 * sin3;
    result.x = cos1 * cos2 * sin3 + sin1 * sin2 * cos3;
    result.y = sin1 * cos2 * cos3 + cos1 * sin2 * sin3;
    result.z = cos1 * sin2 * cos3 - sin1 * cos2 * sin3;

    return result;
}

PlaybackPage::PlaybackPage()
{
    m_pitch = 0;
    m_heading = 0;
    m_rotationdelta = 1.8f;
    InitializeComponent();
    OnLayoutUpdatedToken =
        this->LayoutUpdated += ref new EventHandler<Object^>(this, &PlaybackPage::OnLayoutUpdated);
}

void PlaybackPage::OnLayoutUpdated(Object^ sender, Object^ args)
{
    if (this->m_mtcGrid == nullptr)
    {
        FrameworkElement^ transportControlsTemplateRoot = (FrameworkElement^)(VisualTreeHelper::GetChild(this->VideoElement->TransportControls, 0));
        if (transportControlsTemplateRoot != nullptr)
        {
            this->m_mtcGrid = (Grid^)transportControlsTemplateRoot->FindName("ControlPanelGrid");
            OnMTCPointerPressedToken =
                this->m_mtcGrid->PointerPressed += ref new PointerEventHandler(this, &PlaybackPage::OnPointerPressed);
            OnMTCPointerReleasedToken =
                this->m_mtcGrid->PointerReleased += ref new PointerEventHandler(this, &PlaybackPage::OnPointerReleased);
            OnMTCPointerWheelChangedToken =
                this->m_mtcGrid->PointerWheelChanged += ref new PointerEventHandler(this, &PlaybackPage::OnPointerWheelChanged);
            OnPointerPressedToken =
                this->VideoElement->PointerPressed += ref new PointerEventHandler(this, &PlaybackPage::OnPointerPressed);
            OnPointerReleasedToken =
                this->VideoElement->PointerReleased += ref new PointerEventHandler(this, &PlaybackPage::OnPointerReleased);
            OnPointerMovedToken =
                this->VideoElement->PointerMoved += ref new PointerEventHandler(this, &PlaybackPage::OnPointerMoved);
            OnPointerWheelChangedToken =
                this->VideoElement->PointerWheelChanged += ref new PointerEventHandler(this, &PlaybackPage::OnPointerWheelChanged);
            OnKeyDownToken =
                Window::Current->CoreWindow->KeyDown += ref new TypedEventHandler<CoreWindow^, KeyEventArgs^>(this, &PlaybackPage::OnKeyDown);
        }
    }
}

void PlaybackPage::OnPointerPressed(Object^ sender, PointerRoutedEventArgs^ e)
{
    if (e->OriginalSource != m_mtcGrid && IsMediaAlreadyOpened())
    {
        m_isPointerPress = true;
		m_origin = e->GetCurrentPoint(VideoElement)->Position;
    }
    e->Handled = true;
}


void PlaybackPage::OnPointerReleased(Object^ sender, PointerRoutedEventArgs^ e)
{
    m_isPointerPress = false;
    e->Handled = true;
}


void PlaybackPage::OnPointerWheelChanged(Object^ sender, PointerRoutedEventArgs^ e)
{
    if (e->OriginalSource != m_mtcGrid)
    {
        double value = videoProjection->HorizontalFieldOfViewInDegrees +
            (scrollStep * e->GetCurrentPoint(VideoElement)->Properties->MouseWheelDelta / mouseWheelData);
        if (value > 0 && value <= 180)
        {
            videoProjection->HorizontalFieldOfViewInDegrees = value;
        }
    }
    e->Handled = true;
}


float PlaybackPage::PixelToAngle(float pixel) const
{
	return float(pixel * videoProjection->HorizontalFieldOfViewInDegrees / VideoElement->ActualWidth);
}

void PlaybackPage::OnPointerMoved(Object^ sender, PointerRoutedEventArgs^ e)
{
    if (e->OriginalSource != m_mtcGrid && m_isPointerPress)
    {
        auto current = e->GetCurrentPoint(this->VideoElement)->Position;
	
		m_heading += PixelToAngle(m_origin.X - current.X);
		m_pitch += PixelToAngle(current.Y - m_origin.Y);
		
		this->videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
		m_origin = current;
	}
    e->Handled = true;
}


void PlaybackPage::OnKeyDown(CoreWindow^ sender, KeyEventArgs^ args)
{
    if (IsMediaAlreadyOpened())
    {
        bool isMTCEnabled = this->VideoElement->AreTransportControlsEnabled;

        if (args->VirtualKey == VirtualKey::Up)
        {
            m_pitch = m_pitch + 0.5;
            videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
        }
        if (args->VirtualKey == VirtualKey::Down)
        {
            m_pitch = m_pitch - 0.5;
            videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
        }
        if (args->VirtualKey == VirtualKey::Right)
        {
            m_heading = m_heading + 0.5;
            videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
        }
        if (args->VirtualKey == VirtualKey::Left)
        {
            m_heading = m_heading - 0.5;
            videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
        }
        if (args->VirtualKey == VirtualKey::GamepadA)
        {
            if (!isMTCEnabled)
            {
                this->VideoElement->AreTransportControlsEnabled = true;
                m_inputLoopWorker->Cancel();
                args->Handled = true;
            }

        }
        if (args->VirtualKey == VirtualKey::GamepadB)
        {
            if (isMTCEnabled)
            {
                this->VideoElement->AreTransportControlsEnabled = false;
                StartInputLoop();
                args->Handled = true;
            }
        }
        if (args->VirtualKey == VirtualKey::GamepadLeftTrigger)
        {
            double value = videoProjection->HorizontalFieldOfViewInDegrees + 2;

            if (value <= 180)
            {
                videoProjection->HorizontalFieldOfViewInDegrees = value;
            }
            args->Handled = true;
        }
        if (args->VirtualKey == VirtualKey::GamepadRightTrigger)
        {
            double value = videoProjection->HorizontalFieldOfViewInDegrees - 2;

            if (value > 0)
            {
                videoProjection->HorizontalFieldOfViewInDegrees = value;
            }
            args->Handled = true;
        }
    }
}

void PlaybackPage::OnNavigatedTo(NavigationEventArgs^ e)
{
    SystemNavigationManager::GetForCurrentView()->AppViewBackButtonVisibility = AppViewBackButtonVisibility::Visible;

    Uri^ sourceUri = safe_cast<Uri^>(e->Parameter);
    if (sourceUri != nullptr)
    {
        this->m_player = ref new MediaPlayer();
        MediaSource^ source = MediaSource::CreateFromUri(sourceUri);
        m_player->Source = source;
        OnMediaOpenedToken =
            m_player->MediaOpened += ref new TypedEventHandler<MediaPlayer^, Object^>(this, &PlaybackPage::OnMediaOpened);
        this->VideoElement->SetMediaPlayer(m_player);
    }
}


bool PlaybackPage::IsMediaAlreadyOpened()
{
    if (m_player != nullptr && videoProjection != nullptr &&
        m_player->PlaybackSession->PlaybackState != MediaPlaybackState::Opening &&
        m_player->PlaybackSession->PlaybackState != MediaPlaybackState::None)
    {
        return true;
    }
    return false;
}


void PlaybackPage::ResetState()
{
    m_isPointerPress = false;
    m_pitch = 0;
    m_heading = 0;
}

void PlaybackPage::OnMediaOpened(MediaPlayer^ sender, Object^ args)
{
        videoProjection = m_player->PlaybackSession->SphericalVideoProjection;
        Windows::Media::MediaProperties::SphericalVideoFrameFormat videoFormat = videoProjection->FrameFormat;
        // Some times Content metadata doesn't provided correct format. Try to force the equirectangular format, if doesn't match
        if (videoFormat != Windows::Media::MediaProperties::SphericalVideoFrameFormat::Equirectangular)
        {
            videoProjection->FrameFormat = Windows::Media::MediaProperties::SphericalVideoFrameFormat::Equirectangular;
        }
        videoProjection->IsEnabled = true;
        videoProjection->HorizontalFieldOfViewInDegrees = 120;
        OnPlaybackStateChangedToken =
            m_player->PlaybackSession->PlaybackStateChanged += ref new TypedEventHandler<MediaPlaybackSession^, Object^>(this, &PlaybackPage::OnPlaybackStateChanged);

    StartInputLoop();
}


void PlaybackPage::OnPlaybackStateChanged(MediaPlaybackSession^ sender, Object^ args)
{
    if (m_player->PlaybackSession->PlaybackState == MediaPlaybackState::Playing)
    {
        m_rotationdelta = 1.8f;
    }
    else
    {
        m_rotationdelta = 0.25f;
    }
}

void PlaybackPage::OnNavigatedFrom(NavigationEventArgs^ e)
{
    if (m_player != nullptr)
    {
        if (OnPlaybackStateChangedToken.Value)
        {
            m_player->PlaybackSession->PlaybackStateChanged -= OnPlaybackStateChangedToken;
        }
        if (OnMediaOpenedToken.Value)
        {
            m_player->MediaOpened -= OnMediaOpenedToken;
        }
        m_player->Source = nullptr;
    }

    if (m_mtcGrid != nullptr)
    {
        this->m_mtcGrid->PointerPressed -= OnMTCPointerPressedToken;
        this->m_mtcGrid->PointerReleased -= OnMTCPointerReleasedToken;
        this->m_mtcGrid->PointerWheelChanged -= OnMTCPointerWheelChangedToken;
    }

    if (this->VideoElement != nullptr)
    {
        this->VideoElement->PointerPressed -= OnPointerPressedToken;
        this->VideoElement->PointerReleased -= OnPointerReleasedToken;
        this->VideoElement->PointerMoved -= OnPointerMovedToken;
        this->VideoElement->PointerWheelChanged -= OnPointerWheelChangedToken;
    }

    if (Window::Current != nullptr)
    {
        Window::Current->CoreWindow->KeyDown -= OnKeyDownToken;
    }

    this->LayoutUpdated -= OnLayoutUpdatedToken;

    SystemNavigationManager::GetForCurrentView()->AppViewBackButtonVisibility = AppViewBackButtonVisibility::Collapsed;
    this->VideoElement->SetMediaPlayer(nullptr);
    m_player = nullptr;
}

void  PlaybackPage::StartInputLoop()
{
    if (this->m_inputLoopWorker != nullptr)
    {
        if (m_inputLoopWorker->Status == AsyncStatus::Started)
        {
            return;
        }
        else
        {
            m_inputLoopWorker->Close();
        }
    }

    auto workItem = ref new WorkItemHandler(
        [this](IAsyncAction^ workItem)
    {
        while (workItem->Status == AsyncStatus::Started)
        {
            if (this->m_activeGamePad == nullptr)
            {
                auto gamepads = Gamepad::Gamepads;
                if (gamepads->Size > 0)
                {
                    // Get the Zero based first Gamepad
                    m_activeGamePad = gamepads->GetAt(0);
                }
            }

            if (this->m_activeGamePad != nullptr)
            {
                m_timer.Tick([this]()
                {
                    // Put time-based updates here. By default this code will run once per frame,
                    // but if you change the StepTimer to use a fixed time step this code will
                    // run as many times as needed to get to the current step.
                    //

                    GamepadReading reading = m_activeGamePad->GetCurrentReading();
                    double deltax;
                    if (reading.LeftThumbstickX > THUMBSTICK_DEADZONE || reading.LeftThumbstickX < -THUMBSTICK_DEADZONE)
                    {
                        deltax = reading.LeftThumbstickX * reading.LeftThumbstickX * reading.LeftThumbstickX;
                    }
                    else
                    {
                        deltax = 0.0f;
                    }
                    double deltay;
                    if (reading.LeftThumbstickY > THUMBSTICK_DEADZONE || reading.LeftThumbstickY < -THUMBSTICK_DEADZONE)
                    {
                        deltay = reading.LeftThumbstickY * reading.LeftThumbstickY * reading.LeftThumbstickY;
                    }
                    else
                    {
                        deltay = 0.0f;
                    }

                    double rotationX = deltax * m_rotationdelta;
                    double rotationY = deltay * m_rotationdelta;

                    m_pitch += rotationY;
                    m_heading += rotationX;

                    // Limit pitch to straight up or straight down.
                    this->videoProjection->ViewOrientation = CreateFromHeadingPitchRoll(m_heading, m_pitch, 0);
                });
            }
        }
    });

    auto asyncAction = ThreadPool::RunAsync(workItem);
    m_inputLoopWorker = asyncAction;
}
