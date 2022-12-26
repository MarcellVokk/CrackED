using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

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

            ImmediateRendering = Owner.ImmediateRendering;

            if (ImmediateRendering)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
        }

        public void ReInitialize()
        {
            if (!ImmediateRendering && Owner.ImmediateRendering)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
            else if(ImmediateRendering && !Owner.ImmediateRendering)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }

            ImmediateRendering = Owner.ImmediateRendering;
        }

        private void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            InvalidateVisual();
        }

        public void Repaint(bool repaintSelection)
        {
            if (!ImmediateRendering)
            {
                RepaintRequested = true;
                RepaintSelectionRequested = repaintSelection;

                InvalidateVisual();
            }
        }

        internal bool RepaintRequested = false;
        internal bool RepaintSelectionRequested = false;

        private bool ImmediateRendering = false;
        private int Debug_FrameCount = 0;
        private int Debug_Fps = 0;
        private DateTime Debug_LastSecondFrame;
        private double LongestFrameTime = 0;

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (RepaintRequested || ImmediateRendering)
            {
                RepaintRequested = false;

                Stopwatch sw = Stopwatch.StartNew();
                Owner.TransformDrawingContext(ref drawingContext);

                for (int i = Owner.FirstRenderedLine; i < Math.Min(Owner.Lines.Count, Owner.LinesToRender + Owner.FirstRenderedLine); i++)
                {
                    if (Owner.Lines[i].Content.TextLenght == 0)
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

                if (RepaintSelectionRequested || ImmediateRendering)
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
