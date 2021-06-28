using _360VideoPlayback.Common;
using System;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace _360VideoPlayback
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaybackPage : Page
    {
        private MediaPlayer mediaPlayer;
        // positions hold the start of the movement
        private Point origin;
        // MTC grid to ignore mouse event on the MTC Panel
        Grid mtcGrid = null;
        private double pitch = 0, heading = 0;
        private MediaPlaybackSphericalVideoProjection videoProjection;
        private bool isPointerPress = false;
        IAsyncAction m_inputLoopWorker = null;
        Gamepad activeGamePad = null;
        WorkItemHandler workItemHandler;
        const double THUMBSTICK_DEADZONE = 0.25f;
        //Frame Timer
        private StepTimer timer = new StepTimer();
        double rotationdelta = 1.8f;
        const int scrollStep = 2;
        const int mouseWheelData = 120;

        public PlaybackPage()
        {
            this.InitializeComponent();
            this.LayoutUpdated += PlaybackPage_LayoutUpdated;
        }

        private void PlaybackPage_LayoutUpdated(object sender, object e)
        {
            if (mtcGrid == null)
            {
                FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(VideoElement.TransportControls, 0);
                if (transportControlsTemplateRoot != null)
                {
                    mtcGrid = (Grid)transportControlsTemplateRoot.FindName("ControlPanelGrid");
                    mtcGrid.PointerPressed += OnPointerPressed;
                    mtcGrid.PointerReleased += OnPointerReleased;
                    mtcGrid.PointerWheelChanged += OnPointerWheelChanged;
                    VideoElement.PointerPressed += OnPointerPressed;
                    VideoElement.PointerReleased += OnPointerReleased;
                    VideoElement.PointerMoved += OnPointerMoved;
                    VideoElement.PointerWheelChanged += OnPointerWheelChanged;
                    Window.Current.CoreWindow.KeyDown += OnKeyDown;
                }
            }
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource != mtcGrid)
            {
                double value = videoProjection.HorizontalFieldOfViewInDegrees +
                    (scrollStep * e.GetCurrentPoint(VideoElement).Properties.MouseWheelDelta / mouseWheelData);
                if (value > 0 && value <= 180)
                {
                    videoProjection.HorizontalFieldOfViewInDegrees = value;
                }
            }
            e.Handled = true;
        }

        private void OnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (IsMediaAlreadyOpened())
            {
                bool isMTCEnabled = VideoElement.AreTransportControlsEnabled;

                if (args.VirtualKey == VirtualKey.Up)
                {
                    pitch = pitch + 0.5;
                    videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                }
                if (args.VirtualKey == VirtualKey.Down)
                {
                    pitch = pitch - 0.5;
                    videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                }
                if (args.VirtualKey == VirtualKey.Right)
                {
                    heading = heading + 0.5;
                    videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                }
                if (args.VirtualKey == VirtualKey.Left)
                {
                    heading = heading - 0.5;
                    videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                }
                if (args.VirtualKey == VirtualKey.GamepadA)
                {
                    if (!isMTCEnabled)
                    {
                        VideoElement.AreTransportControlsEnabled = true;
                        m_inputLoopWorker.Cancel();
                        args.Handled = true;
                    }

                }
                if (args.VirtualKey == VirtualKey.GamepadB)
                {
                    if (isMTCEnabled)
                    {
                        VideoElement.AreTransportControlsEnabled = false;
                        StartInputLoop();
                        args.Handled = true;
                    }
                }
                if (args.VirtualKey == VirtualKey.GamepadLeftTrigger)
                {
                    double value = videoProjection.HorizontalFieldOfViewInDegrees + 2;

                    if (value <= 180)
                    {
                        videoProjection.HorizontalFieldOfViewInDegrees = value;
                    }
                    args.Handled = true;
                }
                if (args.VirtualKey == VirtualKey.GamepadRightTrigger)
                {
                    double value = videoProjection.HorizontalFieldOfViewInDegrees - 2;

                    if (value > 0)
                    {
                        videoProjection.HorizontalFieldOfViewInDegrees = value;
                    }
                    args.Handled = true;
                }
            }
        }

        private double PixelToAngle(double pixel) => pixel * videoProjection.HorizontalFieldOfViewInDegrees / VideoElement.ActualWidth;

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource != mtcGrid && isPointerPress)
            {
                var current = e.GetCurrentPoint(VideoElement).Position;

                heading += PixelToAngle(origin.X - current.X);
                pitch += PixelToAngle(current.Y - origin.Y);
                videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                origin = current;
            }
            e.Handled = true;
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isPointerPress = false;
            e.Handled = true;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource != mtcGrid && IsMediaAlreadyOpened())
            {
                isPointerPress = true;
                origin = e.GetCurrentPoint(VideoElement).Position;
            }
            e.Handled = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            Uri sourceUri = (Uri)e.Parameter;
            if (sourceUri != null)
            {
                mediaPlayer = new MediaPlayer();
                MediaSource source = MediaSource.CreateFromUri(sourceUri);
                mediaPlayer.Source = source;
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                VideoElement.SetMediaPlayer(mediaPlayer);
            }
        }

        private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            videoProjection = mediaPlayer.PlaybackSession.SphericalVideoProjection;
            Windows.Media.MediaProperties.SphericalVideoFrameFormat videoFormat = videoProjection.FrameFormat;
            // Some times Content metadata doesn't provided correct format. Try to force the equirectangular format, if doesn't match
            if (videoFormat != Windows.Media.MediaProperties.SphericalVideoFrameFormat.Equirectangular)
            {
                videoProjection.FrameFormat = Windows.Media.MediaProperties.SphericalVideoFrameFormat.Equirectangular;
            }
            videoProjection.IsEnabled = true;
            videoProjection.HorizontalFieldOfViewInDegrees = 120;
            mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            StartInputLoop();
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                rotationdelta = 1.8f;
            }
            else
            {
                rotationdelta = 0.25f;
            }
        }

        private bool IsMediaAlreadyOpened()
        {
            if (mediaPlayer != null && videoProjection != null &&
                mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Opening &&
                mediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.None)
            {
                return true;
            }
            return false;
        }

        private void ResetState()
        {
            isPointerPress = false;
            pitch = 0;
            heading = 0;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
            }
            if (mtcGrid != null)
            {
                mtcGrid.PointerPressed -= OnPointerPressed;
                mtcGrid.PointerReleased -= OnPointerReleased;
                mtcGrid.PointerWheelChanged -= OnPointerWheelChanged;
                VideoElement.PointerPressed -= OnPointerPressed;
                VideoElement.PointerReleased -= OnPointerReleased;
                VideoElement.PointerMoved -= OnPointerMoved;
                VideoElement.PointerWheelChanged -= OnPointerWheelChanged;
                Window.Current.CoreWindow.KeyDown -= OnKeyDown;
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            mediaPlayer.Source = null;
            VideoElement.SetMediaPlayer(null);
            mediaPlayer = null;
        }

        void StartInputLoop()
        {
            // If the animation render loop is already running then do not start another thread.
            if (m_inputLoopWorker != null)
            {
                if (m_inputLoopWorker.Status == AsyncStatus.Started)
                {
                    return;
                }
                else
                {
                    m_inputLoopWorker.Close();
                }
            }

            workItemHandler = new WorkItemHandler((IAsyncAction action) =>
            {
                while (action.Status == AsyncStatus.Started)
                {
                    var gamepads = Gamepad.Gamepads;
                    if (gamepads.Count > 0 && activeGamePad == null)
                    {
                        // Get the Zero based first Gamepad
                        activeGamePad = gamepads[0];
                    }

                    if (activeGamePad != null)
                    {
                        timer.Tick(() =>
                        {
                            GamepadReading reading = activeGamePad.GetCurrentReading();
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

                            double rotationX = deltax * rotationdelta;
                            double rotationY = deltay * rotationdelta;

                            pitch += rotationY;
                            heading += rotationX;

                            // Limit pitch to straight up or straight down.
                            videoProjection.ViewOrientation = Helpers.CreateFromHeadingPitchRoll(heading, pitch, 0);
                        });
                    }
                }
            });

            m_inputLoopWorker = ThreadPool.RunAsync(workItemHandler, WorkItemPriority.High, WorkItemOptions.None);
        }
    }
}
