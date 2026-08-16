using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using WinForms = System.Windows.Forms;

namespace VisionFramework.UI.Controls
{
    /// <summary>
    /// VisionPro 显示控件——封装 CogDisplay + CogRecordDisplay。
    /// 从原 VisionProHostControl 提取的显示逻辑。
    /// </summary>
    public partial class VisionDisplayControl : UserControl
    {
        private readonly CogDisplay _imgDisplay;
        private readonly CogRecordDisplay _recDisplay;

        public VisionDisplayControl()
        {
            InitializeComponent();
            _imgDisplay = new CogDisplay { Dock = WinForms.DockStyle.Fill };
            _recDisplay = new CogRecordDisplay { Dock = WinForms.DockStyle.Fill };
            ImageHost.Child = _imgDisplay;
            RecordHost.Child = _recDisplay;
        }

        public void ShowImage(ICogImage image)
        {
            _imgDisplay.Image = image;
            SafeFit(_imgDisplay);
            ImageHost.Visibility = Visibility.Visible;
            RecordHost.Visibility = Visibility.Collapsed;
        }

        public void ShowRecord(ICogImage image, ICogRecord record)
        {
            if (image != null)
            { try { _recDisplay.Image = image; } catch { } }
            _recDisplay.Record = record;
            SafeFit(_recDisplay);
            RecordHost.Visibility = Visibility.Visible;
            ImageHost.Visibility = Visibility.Collapsed;
        }

        public void Fit()
        {
            if (RecordHost.Visibility == Visibility.Visible) SafeFit(_recDisplay);
            else SafeFit(_imgDisplay);
        }

        private static void SafeFit(object display)
        {
            if (display == null) return;
            try
            {
                var t = display.GetType();
                var fit1 = t.GetMethod("Fit", new[] { typeof(bool) });
                if (fit1 != null) { fit1.Invoke(display, new object[] { true }); return; }
                t.GetMethod("Fit", Type.EmptyTypes)?.Invoke(display, null);
            }
            catch { }
        }
    }
}