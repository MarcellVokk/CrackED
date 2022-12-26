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
        public LineContent Content = new LineContent();

        internal Editor Owner;
        internal List<double> WidthTree = new List<double> { 0d };
        private SortedList<int, StyleSpan> StyleSpans = new SortedList<int, StyleSpan>();

        public VirtualLine(Editor owner)
        {
            Owner = owner;
        }

        public void AddSpan(StyleSpan span)
        {
            StyleSpans.Add(-span.Start, span);
        }

        public void ClearSpans()
        {
            StyleSpans.Clear();
        }

        public double VisualDistanceToIndex(int index)
        {
            if(index == -1) { return WidthTree.LastOrDefault(0); }
            return index < WidthTree.Count ? (index >= 0 ? WidthTree[index] : -1) : -1;
        }

        public void Draw(DrawingContext drawingContext, int visualPosition)
        {
            WidthTree.Clear();

            double XOffset = 0;
            int DrawnUntil = 0;
            string Text = Content.ToText();
            bool BreakFlag = false;

            int c = 0;

            Stack<StyleSpan> StyleStack = new Stack<StyleSpan>(StyleSpans.Values);
            StyleSpan? CurentStyle;
            Pop();

            while (!BreakFlag)
            {
                if (CurentStyle != null && CurentStyle.Start == DrawnUntil)
                {
                    c++;
                    drawingContext.DrawGlyphRun(CurentStyle.Foreground, GetNextSection(ValidateCurentStyleLenght(), GetTypeface(CurentStyle)));
                    DrawnUntil = CurentStyle.Start + ValidateCurentStyleLenght();

                    Pop();
                }
                else if (CurentStyle != null)
                {
                    c++;
                    drawingContext.DrawGlyphRun(Owner.Foreground, GetNextSection(CurentStyle.Start - DrawnUntil, Owner.Typeface_Regular));
                    DrawnUntil = CurentStyle.Start;
                }

                if (CurentStyle == null && !BreakFlag)
                {
                    c++;
                    drawingContext.DrawGlyphRun(Owner.Foreground, GetNextSection(Text.Length - DrawnUntil, Owner.Typeface_Regular));
                    break;
                }
            }

            WidthTree.Add(XOffset);


            void Pop()
            {
                if (StyleStack.Count > 0)
                {
                    CurentStyle = StyleStack.Pop();

                    if (CurentStyle.Start < DrawnUntil)
                    {
                        Pop();
                    }
                }
                else
                {
                    CurentStyle = null;
                }
            }

            GlyphRun GetNextSection(int sectionLenght, GlyphTypeface typeface)
            {
                List<ushort> glyphIndices = new List<ushort>();
                List<double> advanceWidths = new List<double>();
                double sectionWidth = 0;

                for (int i = DrawnUntil; i < DrawnUntil + sectionLenght; ++i)
                {
                    if (i >= Text.Length || i > 10000)
                    {
                        BreakFlag = true;
                        break;
                    }

                    ushort glyphIndex = typeface.CharacterToGlyphMap['?'];
                    if (typeface.CharacterToGlyphMap.ContainsKey(Text[i]))
                    {
                        glyphIndex = typeface.CharacterToGlyphMap[Text[i]];
                    }
                    double advanceWidth = typeface.AdvanceWidths[glyphIndex] * Owner.FontSize;

                    glyphIndices.Add(glyphIndex);
                    advanceWidths.Add(advanceWidth);

                    WidthTree.Add(XOffset + sectionWidth);
                    sectionWidth += advanceWidth;

                    if (XOffset + sectionWidth > Owner.RenderArea.ActualWidth - Owner.HorizontalTransform)
                    {
                        BreakFlag = true;
                        Debug.WriteLine("break");
                        break;
                    }
                }

                if (glyphIndices.Count > 0)
                {
                    GlyphRun gl = new GlyphRun
                      (
                          typeface,
                          bidiLevel: 0,
                          isSideways: false,
                          renderingEmSize: Owner.FontSize,
                          pixelsPerDip: (float)VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip,   //  Only in higher versions  .NET  Only this parameter 
                          glyphIndices: glyphIndices,
                          baselineOrigin: new Point(XOffset, (visualPosition - Owner.FirstRenderedLine) * Owner.LineHeight + (typeface.Height * Owner.FontSize) + Owner.LineHeight / 2 - typeface.Height * Owner.FontSize / 2 + Owner.VerticalTextOffset),     //  Set the offset of the text 
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

            int ValidateCurentStyleLenght()
            {
                int result = CurentStyle.Lenght == -1 ? Text.Length - CurentStyle.Start : CurentStyle.Lenght;

                return result;
            }
        }

        private GlyphTypeface GetTypeface(StyleSpan styleSpan)
        {
            if (styleSpan.IsBold)
            {
                if (styleSpan.IsItalic)
                {
                    return Owner.Typeface_BoldItalic;
                }
                else
                {
                    return Owner.Typeface_Bold;
                }
            }
            else
            {
                if (styleSpan.IsItalic)
                {
                    return Owner.Typeface_RegularItalic;
                }
                else
                {
                    return Owner.Typeface_Regular;
                }
            }
        }
    }
}
