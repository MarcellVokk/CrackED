using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CrackED
{
    public class VirtualLine
    {
        public Editor Owner;
        public List<string> Chars = new List<string>();
        public List<double> WidthTree = new List<double>();
        public List<StyleSpan> StyleSpans = new List<StyleSpan>();

        public VirtualLine(Editor owner)
        {
            Owner = owner;

            StyleSpans.Add(new StyleSpan(0, 1, Brushes.Red));
            StyleSpans.Add(new StyleSpan(1, 1, Brushes.Green));
            StyleSpans.Add(new StyleSpan(2, 1, Brushes.Blue));
            StyleSpans.Add(new StyleSpan(3, 1, Brushes.Orange));
            StyleSpans.Add(new StyleSpan(4, 1, Brushes.Olive));
            StyleSpans.Add(new StyleSpan(5, 1, Brushes.Lime));
            StyleSpans.Add(new StyleSpan(6, 1, Brushes.Lavender));
            StyleSpans.Add(new StyleSpan(7, 1, Brushes.SkyBlue));
            StyleSpans.Add(new StyleSpan(8, 1, Brushes.OrangeRed));
            StyleSpans.Add(new StyleSpan(9, 1, Brushes.DarkCyan));
            StyleSpans.Add(new StyleSpan(10, 1, Brushes.Crimson));
        }

        public double VisualDistanceToIndex(int index)
        {
            if(WidthTree.Count == 0) { return 0; }
            return index <= WidthTree.Count ? (index >= 0 ? WidthTree[index] : 0) : Owner.RenderArea.ActualWidth;
        }

        public override string ToString()
        {
            return string.Join("", Chars);
        }

        public bool IsOffsetVisible(int offset)
        {
            return WidthTree.Count > offset;
        }

        public void Draw(DrawingContext drawingContext, int visualPosition)
        {
            WidthTree.Clear();

            Point Baseline = new Point(0, Owner.Typeface.Baseline * Owner.FontSize);
            double XOffset = 0;
            int DrawnUntil = 0;
            string Text = this.ToString();
            bool BreakFlag = false;

            Stack<StyleSpan> StyleStack = new Stack<StyleSpan>(this.StyleSpans.OrderByDescending(x => x.Start));
            StyleSpan? CurentStyle;
            Pop();

            while (!BreakFlag)
            {
                if (CurentStyle != null && CurentStyle.Start == DrawnUntil)
                {
                    drawingContext.DrawGlyphRun(CurentStyle.Foreground, GetNextSection(CurentStyle.Lenght));
                    DrawnUntil = CurentStyle.Start + CurentStyle.Lenght;

                    Pop();
                }
                else if (CurentStyle != null)
                {
                    drawingContext.DrawGlyphRun(Owner.Foreground, GetNextSection(CurentStyle.Start - DrawnUntil));
                    DrawnUntil = CurentStyle.Start;
                }

                if (CurentStyle == null && !BreakFlag)
                {
                    drawingContext.DrawGlyphRun(Owner.Foreground, GetNextSection(Text.Length - DrawnUntil));
                    break;
                }
            }

            WidthTree.Add(XOffset);



            void Pop()
            {
                if (StyleStack.Count > 0)
                {
                    CurentStyle = StyleStack.Pop();
                }
                else
                {
                    CurentStyle = null;
                }
            }

            GlyphRun GetNextSection(int sectionLenght)
            {
                List<ushort> glyphIndices = new List<ushort>();
                List<Point> glyphOffsets = new List<Point>();
                List<double> advanceWidths = new List<double>();

                for (int i = DrawnUntil; i < DrawnUntil + sectionLenght; ++i)
                {
                    if(i >= Text.Length)
                    {
                        BreakFlag = true;
                        break;
                    }

                    ushort glyphIndex = Owner.Typeface.CharacterToGlyphMap['?'];
                    if (Owner.Typeface.CharacterToGlyphMap.ContainsKey(Text[i]))
                    {
                        glyphIndex = Owner.Typeface.CharacterToGlyphMap[Text[i]];
                    }
                    double advanceWidth = Owner.Typeface.AdvanceWidths[glyphIndex] * Owner.FontSize;

                    glyphIndices.Add(glyphIndex);
                    advanceWidths.Add(0);
                    glyphOffsets.Add(new Point(XOffset, -((visualPosition - Owner.FirstVisibleLine) * Owner.LineHeight) - Owner.LineHeight / 2 + (Owner.Typeface.Baseline * Owner.FontSize) / 2));

                    WidthTree.Add(XOffset);
                    XOffset += advanceWidth;

                    if (XOffset > Owner.RenderArea.ActualWidth - Owner.HorizontalTransform)
                    {
                        BreakFlag = true;
                        break;
                    }
                }

                if(glyphIndices.Count > 0)
                {
                    return new GlyphRun(
                    Owner.Typeface,
                    0,
                    false,
                    Owner.FontSize,
                    glyphIndices,
                    Baseline,
                    advanceWidths,
                    glyphOffsets,
                    null,
                    null,
                    null,
                    null,
                    null);
                }

                return null;
            }
        }
    }
}
