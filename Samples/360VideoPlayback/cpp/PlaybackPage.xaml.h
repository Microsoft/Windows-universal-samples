//
// PlaybackPage.xaml.h
// Declaration of the PlaybackPage class
//

#pragma once

#include "PlaybackPage.g.h"
#include "Common\StepTimer.h"

namespace _360VideoPlayback
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public ref class PlaybackPage sealed
    {
    public:
        PlaybackPage();

    protected:
        void OnNavigatedTo(Windows::UI::Xaml::Navigation::NavigationEventArgs^ e) override;
        void OnNavigatedFrom(Windows::UI::Xaml::Navigation::NavigationEventArgs^ e) override;

    private:

        Windows::Media::Playback::MediaPlayer^ m_player;
        // positions hold the origin of the movement
		Windows::Foundation::Point m_origin;
        // MTC grid to ignore mouse event on the MTC Panel
        Windows::UI::Xaml::Controls::Grid^ m_mtcGrid;
        double m_pitch;
        double m_heading;
        // Framebased timer.
        DX::StepTimer m_timer;

        Windows::Media::Playback::MediaPlaybackSphericalVideoProjection^ videoProjection;
        bool m_isPointerPress = false;
        Windows::Foundation::IAsyncAction^ m_inputLoopWorker;
        Windows::Gaming::Input::Gamepad^ m_activeGamePad;
        double m_rotationdelta;
        Windows::Foundation::EventRegistrationToken OnLayoutUpdatedToken;
        void OnLayoutUpdated(Platform::Object^ sender, Platform::Object^ args);
        bool IsMediaAlreadyOpened();
        void ResetState();
        void StartInputLoop();

		float PixelToAngle(float pixel) const;
        void OnPointerPressed(Platform::Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ e);
        Windows::Foundation::EventRegistrationToken OnPointerPressedToken;
        Windows::Foundation::EventRegistrationToken OnMTCPointerPressedToken;
        void OnPointerReleased(Platform::Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ e);
        Windows::Foundation::EventRegistrationToken OnPointerReleasedToken;
        Windows::Foundation::EventRegistrationToken OnMTCPointerReleasedToken;
        void OnPointerWheelChanged(Platform::Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ e);
        Windows::Foundation::EventRegistrationToken OnPointerWheelChangedToken;
        Windows::Foundation::EventRegistrationToken OnMTCPointerWheelChangedToken;
        void OnPointerMoved(Platform::Object^ sender, Windows::UI::Xaml::Input::PointerRoutedEventArgs^ e);
        Windows::Foundation::EventRegistrationToken OnPointerMovedToken;
        Windows::Foundation::EventRegistrationToken OnMTCPointerMovedToken;
        void OnKeyDown(Windows::UI::Core::CoreWindow^ sender, Windows::UI::Core::KeyEventArgs^ args);
        Windows::Foundation::EventRegistrationToken OnKeyDownToken;
        void OnMediaOpened(Windows::Media::Playback::MediaPlayer^ sender, Platform::Object^ args);
        Windows::Foundation::EventRegistrationToken OnMediaOpenedToken;
        void OnPlaybackStateChanged(Windows::Media::Playback::MediaPlaybackSession^ sender, Platform::Object^ args);
        Windows::Foundation::EventRegistrationToken OnPlaybackStateChangedToken;
    };
}
