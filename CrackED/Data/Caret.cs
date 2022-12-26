using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace CrackED
{
    public class Caret
    {
        public enum CaretState
        {
            SelectionActive,
            SelectionVisible,
            CaretVisible
        }

        #region Constructor
        public Caret(Editor owner)
        {
            Owner = owner;

            Caret_blink.Interval = Owner.CaretBlinkInterval;
            Caret_blink.Elapsed += (sender, e) =>
            {
                IsVisible = !IsVisible;

                Owner.CaretLayer.Dispatcher.Invoke(() => Owner.CaretLayer.Repaint());
            };
            Caret_blink.Start();

            Caret_blink_timeout.Interval = Owner.CaretBlinkTimeout;
            Caret_blink_timeout.Elapsed += (sender, e) =>
            {
                Caret_blink.Start();
            };
        }
        #endregion

        #region Internal fields
        internal bool IsVisible = true;

        internal System.Timers.Timer Caret_blink = new System.Timers.Timer();
        internal System.Timers.Timer Caret_blink_timeout = new System.Timers.Timer();

        internal Editor Owner;

        internal CaretState MainCaretState = CaretState.CaretVisible;

        private int SelectionStartOffset = 0;
        private int SelectionStartLine = 0;
        #endregion

        #region Public fields
        public int UnclampedOffset { get; private set; } = 0;
        public int CurentOffset { get; private set; } = 0;
        public int CurentLine { get; private set; } = 0;
        #endregion

        #region Helper functions
        public void SetPosition(int offset, int line)
        {
            DocumentPosition validatedPosition = ValidatePosition(offset, line);

            PauseCaretBlink();

            CurentLine = validatedPosition.Line;
            CurentOffset = validatedPosition.Offset;
            UnclampedOffset = offset;

            Owner.SelectionLayer.Repaint();
            Owner.CaretLayer.Repaint();

            ResumeCaretBlink();
        }

        public void ScrollIntoView()
        {
            if(CurentLine + 2 > Owner.FirstRenderedLine + (Owner.LinesToRender - Owner.ExtraLines))
            {
                Owner.SetVerticalOffset((CurentLine + 2 - (Owner.LinesToRender - Owner.ExtraLines)) * Owner.LineHeight);
            }
            else if (CurentLine - 1 < Owner.FirstRenderedLine)
            {
                Owner.SetVerticalOffset((CurentLine - 1) * Owner.LineHeight);
            }
        }

        internal void PauseCaretBlink()
        {
            Caret_blink_timeout.Stop();
            Caret_blink.Stop();
            IsVisible = true;
            Owner.CaretLayer.Repaint();
        }

        internal void ResumeCaretBlink()
        {
            Caret_blink_timeout.Stop();
            Caret_blink_timeout.Start();
        }

        public DocumentPosition ValidatePosition(int offset, int line)
        {
            line = Math.Min(Math.Max(0, line), Owner.Lines.Count - 1);
            offset = Math.Min(Math.Max(0, offset), Owner.Lines[line].Content.TextLenght);

            return new DocumentPosition(offset, line);
        }

        public Selection? GetCurentSelection()
        {
            if (MainCaretState == CaretState.CaretVisible) return null;

            if (SelectionStartLine > CurentLine)
            {
                return new Selection(CurentOffset, CurentLine, SelectionStartOffset, SelectionStartLine, false);
            }
            else if (SelectionStartLine < CurentLine)
            {
                return new Selection(SelectionStartOffset, SelectionStartLine, CurentOffset, CurentLine, true);
            }
            else if (SelectionStartLine == CurentLine)
            {
                if (SelectionStartOffset > CurentOffset)
                {
                    return new Selection(CurentOffset, CurentLine, SelectionStartOffset, CurentLine, false);
                }
                else if (SelectionStartOffset < CurentOffset)
                {
                    return new Selection(SelectionStartOffset, CurentLine, CurentOffset, CurentLine, true);
                }
            }

            return null;
        }
        #endregion

        #region Rendreing
        public void DrawCaret(ref DrawingContext drawingContext)
        {
            if(Owner.IsDocumentPositionInView(CurentOffset, CurentLine))
            {
                drawingContext.DrawRectangle(Owner.CaretBrush, new Pen(Brushes.Black, 0), new Rect(Owner.Lines[CurentLine].VisualDistanceToIndex(CurentOffset), (CurentLine - Owner.FirstRenderedLine) * Owner.LineHeight, Owner.CaretWidth, Owner.LineHeight));
            }
        }

        public void DrawSelection(ref DrawingContext drawingContext)
        {
            int lineSegmentsRendered = 0;

            if (MainCaretState == CaretState.SelectionActive || MainCaretState == CaretState.SelectionVisible)
            {
                PathGeometry g = new PathGeometry();

                Selection? selection = GetCurentSelection();
                
                if(selection == null)
                {
                    return;
                }

                Stopwatch sw = Stopwatch.StartNew();

                double startX = Owner.Lines[selection.Value.StartLine].VisualDistanceToIndex(selection.Value.StartOffset);
                double endX = Owner.Lines[selection.Value.EndLine].VisualDistanceToIndex(selection.Value.EndOffset);

                for (int i = Math.Max(selection.Value.StartLine - Owner.FirstRenderedLine, 0); i <= Math.Min(selection.Value.EndLine - Owner.FirstRenderedLine, Owner.LinesToRender - 1); i++)
                {
                    if(selection.Value.StartLine == selection.Value.EndLine)
                    {
                        g.AddGeometry(new RectangleGeometry(new Rect(new Point(startX, i * Owner.LineHeight), new Point(endX, (i + 1) * Owner.LineHeight)), 0, 0));
                        lineSegmentsRendered++;
                        break;
                    }

                    double curent_lineWidth = Owner.FullWidthSelections ? Owner.RenderArea.ActualWidth : Owner.Lines[Owner.FirstRenderedLine + i].VisualDistanceToIndex(-1);

                    if (i == (selection.Value.StartLine - Owner.FirstRenderedLine))
                    {
                        g.AddGeometry(new RectangleGeometry(new Rect(new Point(startX, i * Owner.LineHeight), new Point(curent_lineWidth, (i + 1) * Owner.LineHeight)), 0, 0));
                        lineSegmentsRendered++;
                    }
                    else if (i == (selection.Value.EndLine - Owner.FirstRenderedLine))
                    {
                        g.AddGeometry(new RectangleGeometry(new Rect(new Point(0, i * Owner.LineHeight), new Point(endX, (i + 1) * Owner.LineHeight)), 0, 0));
                        lineSegmentsRendered++;
                    }
                    else
                    {
                        g.AddGeometry(new RectangleGeometry(new Rect(new Point(0, i * Owner.LineHeight), new Point(Math.Max(curent_lineWidth, Owner.MinSelectionWidth), (i + 1) * Owner.LineHeight)), 0, 0));
                        lineSegmentsRendered++;
                    }
                }

                drawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(130, 73, 158, 255)), new Pen(Brushes.Black, 0), g);

                sw.Stop();

                Owner.DrawMetrics(lineSegmentsRendered.ToString() + " segments in " + sw.Elapsed.TotalMilliseconds + " ms");
            }
        }
        #endregion

        #region Selection
        public void BeginSelection(int startOffset, int startLine)
        {
            if (MainCaretState == CaretState.SelectionVisible || MainCaretState == CaretState.CaretVisible)
            {
                MainCaretState = CaretState.SelectionActive;
                Debug.WriteLine("SelectionActive");
            }

            DocumentPosition validatedPosition = ValidatePosition(startOffset, startLine);

            SelectionStartOffset = validatedPosition.Offset;
            SelectionStartLine = validatedPosition.Line;

            SetPosition(startOffset, startLine);
        }

        public void EndSelection()
        {
            if (MainCaretState == CaretState.SelectionActive && (CurentOffset != SelectionStartOffset || CurentLine != SelectionStartLine))
            {
                MainCaretState = CaretState.SelectionVisible;
                Debug.WriteLine("SelectionVisible");
            }
            else if (MainCaretState == CaretState.SelectionActive || MainCaretState == CaretState.SelectionVisible)
            {
                MainCaretState = CaretState.CaretVisible;
                Debug.WriteLine("CaretVisible");
            }

            //Owner.SelectionLayer.Repaint();
        }

        public void SetSelection(int startOffset, int startLine, int endOffset, int endLine)
        {
            MainCaretState = CaretState.SelectionVisible;
            Debug.WriteLine("SelectionVisible");

            DocumentPosition validatedPosition_start = ValidatePosition(startOffset, startLine);

            SelectionStartOffset = validatedPosition_start.Offset;
            SelectionStartLine = validatedPosition_start.Line;

            DocumentPosition validatedPosition_end = ValidatePosition(endOffset, endLine);

            SetPosition(validatedPosition_end.Offset, validatedPosition_end.Line);

            Owner.SelectionLayer.Repaint();
        }
        #endregion
    }

    public struct Selection
    {
        public int StartOffset { get; set; }
        public int StartLine { get; set; }
        public int EndOffset { get; set; }
        public int EndLine { get; set; }
        public bool IsToLeft { get; set; }

        public Selection(int startOffset, int startLine, int endOffset, int endLine, bool isToLeft)
        {
            StartOffset = startOffset;
            StartLine = startLine;
            EndOffset = endOffset;
            EndLine = endLine;
            IsToLeft = isToLeft;
        }
    }

    public struct DocumentPosition
    {
        public int Offset { get; set; }
        public int Line { get; set; }

        public DocumentPosition(int offset, int line)
        {
            Offset = offset;
            Line = line;
        }
    }
}
