using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
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
        }

        public double VisualDistanceToIndex(int index)
        {
            if(WidthTree.Count == 0) { return 0; }
            return index < WidthTree.Count ? (index >= 0 ? WidthTree[index] : 0) : Owner.RenderArea.ActualWidth;
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
                List<double> advanceWidths = new List<double>();
                double sectionWidth = 0;

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
                    advanceWidths.Add(advanceWidth);

                    WidthTree.Add(XOffset + sectionWidth);
                    sectionWidth += advanceWidth;

                    if (XOffset > Owner.RenderArea.ActualWidth - Owner.HorizontalTransform)
                    {
                        BreakFlag = true;
                        break;
                    }
                }

                if(glyphIndices.Count > 0)
                {
                    GlyphRun gl = new GlyphRun
                      (
                          Owner.Typeface,
                          bidiLevel: 0,
                          isSideways: false,
                          renderingEmSize: Owner.FontSize,
                          pixelsPerDip: (float)VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip,   //  Only in higher versions  .NET  Only this parameter 
                          glyphIndices: glyphIndices,
                          baselineOrigin: new Point(XOffset, (visualPosition - Owner.FirstVisibleLine) * Owner.LineHeight + (Owner.Typeface.Height * Owner.FontSize) + Owner.LineHeight / 2 - Owner.Typeface.Height * Owner.FontSize / 2 + Owner.VerticalTextOffset),     //  Set the offset of the text 
                          advanceWidths: advanceWidths, //  Set the word width of each character , That's the brand name 
                          glyphOffsets: null,           //  Set the offset of each character , Can be null 
                          characters: null,
                          deviceFontName: null,
                          clusterMap: null,
                          caretStops: null,
                          language: XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag)
                      );

                    XOffset += sectionWidth;

                    return gl;
                }

                return null;
            }
        }
    }
}
