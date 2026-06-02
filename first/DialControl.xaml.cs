using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WPF 
{
    public partial class DialControl : UserControl
    {
        public DialControl()
        {
            InitializeComponent();
        }

        public void UpdateAngle(double angle)
        {
            PointerTransform.Angle = angle;
        }
    }
}