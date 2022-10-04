using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CrackED
{
    /// <summary>
    /// Interaction logic for MetricsLayer.xaml
    /// </summary>
    public partial class MetricsLayer : UserControl
    {
        internal Editor Owner;

        public MetricsLayer(Editor owner)
        {
            InitializeComponent();

            Owner = owner;
        }

        public void Repaint(string text)
        {
            Text.Insert(0, text);
            RepaintRequested = true;
            InvalidateVisual();
        }

        private List<string> Text = new List<string>();
        private bool RepaintRequested = false;
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (RepaintRequested)
            {
                RepaintRequested = false;

                for(int i = 0; i < Math.Min(3, Text.Count); i++)
                {
                    FormattedText text = new FormattedText(Text[i],
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Consolas"),
                        12, System.Windows.Media.Brushes.Lime, 1);

                    drawingContext.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 0), new Rect(this.ActualWidth - text.Width, i * text.Height, text.Width, text.Height));
                    drawingContext.DrawText(text, new Point(this.ActualWidth - text.Width, i * text.Height));
                }
            }
        }
    }
}
