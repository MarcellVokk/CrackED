using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CrackED
{
    public class TextSearchManagger
    {
        internal BackgroundLayer Owner;

        public string SearchTerm = "";
        public Brush HiglightBrush = new SolidColorBrush(Color.FromArgb(100, 255, 165, 0));

        public TextSearchManagger(BackgroundLayer owner)
        {
            Owner = owner;
        }

        public void Draw(ref DrawingContext drawingContext)
        {
            if(SearchTerm.Length == 0)
            {
                return;
            }
            
            PathGeometry Geometry = new PathGeometry();

            double YOffset = 0;
            for (int i = Owner.Owner.FirstVisibleLine; i < Math.Min(Owner.Owner.Lines.Count, Owner.Owner.LinesOnScreen + Owner.Owner.FirstVisibleLine); i++)
            {
                string Text = Owner.Owner.Lines[i].ToString();
                int NextIndex = Text.IndexOf(SearchTerm, 0);
                double XOffset;

                while (NextIndex != -1 && Owner.Owner.Lines[i].IsOffsetVisible(NextIndex + SearchTerm.Length))
                {
                    XOffset = Owner.Owner.Lines[i].VisualDistanceToIndex(NextIndex);

                    NextIndex = GetLastOfSame(NextIndex);

                    Geometry.AddGeometry(new RectangleGeometry(new Rect(XOffset, YOffset, Owner.Owner.Lines[i].VisualDistanceToIndex(NextIndex + SearchTerm.Length) - XOffset, Owner.Owner.LineHeight)));

                    NextIndex = Text.IndexOf(SearchTerm, NextIndex + 1);
                }

                YOffset += Owner.Owner.LineHeight;

                if (i % 10 == 0)
                {
                    drawingContext.DrawGeometry(HiglightBrush, new Pen(Brushes.Black, 0), Geometry);
                    Geometry = new PathGeometry();
                }

                int GetLastOfSame(int startIndex)
                {
                    int result = startIndex;

                    while (Owner.Owner.Lines[i].IsOffsetVisible(result + SearchTerm.Length + 1))
                    {
                        int next = Text.IndexOf(SearchTerm, result + SearchTerm.Length);

                        if (next != -1 && next - result == SearchTerm.Length)
                        {
                            result = next;
                        }
                        else
                        {
                            break;
                        }
                    }

                    return result;
                }
            }

            drawingContext.DrawGeometry(HiglightBrush, new Pen(Brushes.Black, 0), Geometry);
        }
    }
}
