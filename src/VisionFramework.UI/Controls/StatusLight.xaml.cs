using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace VisionFramework.UI.Controls
{
    public enum LightState { Off, Green, Yellow, Red }

    public partial class StatusLight : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatusLight), new PropertyMetadata(""));

        public static readonly DependencyProperty OffColorProperty =
            DependencyProperty.Register(nameof(OffColor), typeof(Brush), typeof(StatusLight), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))));

        private LightState _state = LightState.Off;
        private DispatcherTimer _blinkTimer;
        private bool _blinkVisible = true;

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Brush OffColor
        {
            get => (Brush)GetValue(OffColorProperty);
            set => SetValue(OffColorProperty, value);
        }

        public LightState State
        {
            get => _state;
            set
            {
                _state = value;
                UpdateLight();
            }
        }

        public bool IsBlinking { get; set; }

        public StatusLight()
        {
            InitializeComponent();
        }

        public void SetState(LightState state, bool blink = false)
        {
            _state = state;
            IsBlinking = blink;
            if (blink && _blinkTimer == null)
            {
                _blinkTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(500) };
                _blinkTimer.Tick += (s, e) => { _blinkVisible = !_blinkVisible; UpdateLight(); };
            }
            if (blink) _blinkTimer?.Start();
            else _blinkTimer?.Stop();
            _blinkVisible = true;
            UpdateLight();
        }

        private void UpdateLight()
        {
            if (!_blinkVisible)
            {
                Light.Fill = OffColor;
                return;
            }

            switch (_state)
            {
                case LightState.Green:
                    Light.Fill = new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76));
                    break;
                case LightState.Yellow:
                    Light.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xD6, 0x00));
                    break;
                case LightState.Red:
                    Light.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x3B, 0x30));
                    break;
                default:
                    Light.Fill = OffColor;
                    break;
            }
        }
    }
}
