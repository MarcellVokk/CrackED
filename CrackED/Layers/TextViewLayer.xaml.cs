using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for TextViewLayer.xaml
    /// </summary>
    public partial class TextViewLayer : UserControl
    {
        internal Editor Owner;

        public TextViewLayer(Editor owner)
        {
            InitializeComponent();

            Owner = owner;
        }

        public void Repaint(bool repaintSelection)
        {
            RepaintRequested = true;
            RepaintSelectionRequested = repaintSelection;

            InvalidateVisual();
        }

        private bool RepaintRequested = false;
        private bool RepaintSelectionRequested = false;

        private int Debug_FrameCount = 0;
        private int Debug_Fps = 0;
        private DateTime Debug_LastSecondFrame;
        private double LongestFrameTime = 0;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (RepaintRequested)
            {
                RepaintRequested = false;

                Owner.TransformDrawingContext(ref drawingContext);

                Debug.WriteLine("Render: TextViewLayer");

                Stopwatch sw = Stopwatch.StartNew();

                for (int i = Owner.FirstVisibleLine; i < Math.Min(Owner.Lines.Count, Owner.LinesOnScreen + Owner.FirstVisibleLine); i++)
                {
                    if (Owner.Lines[i].Chars.Count == 0)
                    {
                        continue;
                    }

                    Owner.Lines[i].Draw(drawingContext, i);
                }

                sw.Stop();
                LongestFrameTime = sw.Elapsed.TotalMilliseconds > LongestFrameTime ? sw.Elapsed.TotalMilliseconds : LongestFrameTime;

                Debug_FrameCount++;

                if (DateTime.Now > Debug_LastSecondFrame.AddSeconds(1))
                {
                    Debug_Fps = Debug_FrameCount;
                    Debug_FrameCount = 0;
                    Debug_LastSecondFrame = DateTime.Now;
                }

                Owner.DrawMetrics(Debug_Fps + " fps (longest frame: " + LongestFrameTime + " ms)");

                if (RepaintSelectionRequested)
                {
                    RepaintSelectionRequested = false;
                    Owner.SelectionLayer.Repaint();
                }

                Owner.CaretLayer.Repaint();
                Owner.BackgroundLayer.Repaint();
            }
        }
    }
}
