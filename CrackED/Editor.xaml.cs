using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CrackED
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : UserControl
    {
        #region Internal fields
        internal int FirstVisibleLine = 0;
        internal int LinesOnScreen = 30;

        private bool IsAnimating = false;

        private double ActualVerticalOffset = 0d;

        internal double VerticalTransform = 0d;
        internal double HorizontalTransform = 50d;

        internal GlyphTypeface Typeface;

        internal SelectionLayer SelectionLayer;
        internal TextViewLayer TextViewLayer;
        internal CaretLayer CaretLayer;
        internal BackgroundLayer BackgroundLayer;
        internal MetricsLayer MetricsLayer;

        internal List<VirtualLine> Lines = new List<VirtualLine>();
        #endregion

        #region Public fields
        public Caret Caret;

        public new double FontSize = 14;
        public new FontFamily FontFamily;
        public new Brush Foreground = new SolidColorBrush(Color.FromArgb(255, 219, 219, 228));
        public new Brush Background = new SolidColorBrush(Color.FromArgb(255, 38, 38, 38));

        public double LineHeight = 24;
        public double VerticalTextOffset = -4;

        public double MinScrollAnimationSpeed = 360d;
        public double MaxScrollAnimationSpeed = 720d;
        public double LinesToScroll = 3;
        public double ScrollInertia = 1.3d;
        public bool AnimateScroll = true;

        public Brush CaretBrush = new SolidColorBrush(Color.FromArgb(255, 223, 202, 94));
        public double CaretWidth = 2;
        public double CaretBlinkInterval = 500d;
        public double CaretBlinkTimeout = 1000d;

        public double MinSelectionWidth = 5d;
        public bool FullWidthSelections = false;
        #endregion

        #region Public Functions
        public void SerachForAll(string keyword)
        {
            BackgroundLayer.TextSearchManagger.SearchTerm = keyword;
            BackgroundLayer.Repaint();
        }

        public void AddStyleSpan(int line, StyleSpan styleSpan)
        {
            Lines[line].StyleSpans.Add(styleSpan);
        }

        public void ClearStyleSpans(int line)
        {
            Lines[line].StyleSpans.Clear();
        }

        public void ClearStyleSpans()
        {
            for(int i = 0; i < Lines.Count; i++)
            {
                Lines[i].StyleSpans.Clear();
            }
        }

        public void UpdateOptions()
        {
            Caret.Caret_blink.Interval = CaretBlinkInterval;
            Caret.Caret_blink_timeout.Interval = CaretBlinkTimeout;

            VerticalOffset = (double)GetValue(VerticalOffsetProperty);
            ActualVerticalOffset = VerticalOffset;

            if (IsAnimating)
            {
                BeginAnimation(VerticalOffsetProperty, null);
                IsAnimating = false;
            }

            TextViewLayer.Repaint(true);
        }
        #endregion

        
        #region Constructor
        public Editor()
        {
            InitializeComponent();

            Caret = new Caret(this);
            SelectionLayer = new SelectionLayer(this);
            TextViewLayer = new TextViewLayer(this);
            CaretLayer = new CaretLayer(this);
            BackgroundLayer = new BackgroundLayer(this);
            MetricsLayer = new MetricsLayer(this);

            RenderArea.Children.Add(BackgroundLayer);
            RenderArea.Children.Add(SelectionLayer);
            RenderArea.Children.Add(TextViewLayer);
            RenderArea.Children.Add(CaretLayer);
            RenderArea.Children.Add(MetricsLayer);

            Lines.Add(new VirtualLine(this));

            Typeface typeface = new Typeface(new FontFamily("Hack"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            if (!typeface.TryGetGlyphTypeface(out Typeface))
            {
                return;
            }
        }
        #endregion

        #region Helpers

        public DocumentPosition DocumentPositionFromScreenPosition(double x, double y)
        {
            y -= VerticalTransform;
            x -= HorizontalTransform;

            int line = Math.Min((int)(y / LineHeight) + FirstVisibleLine, Lines.Count - 1);

            int offset = 0;
            double screenOffset = 0;

            while (offset < Lines[line].Chars.Count)
            {
                double next_screenOffset = Lines[line].VisualDistanceToIndex(offset + 1);

                if (screenOffset > x - (next_screenOffset - screenOffset) / 2)
                {
                    break;
                }

                screenOffset = next_screenOffset;
                offset++;
            }

            return new DocumentPosition(offset, line);
        }

        public bool IsDocumentPositionInView(int offset, int line)
        {
            return line >= FirstVisibleLine && line < FirstVisibleLine + LinesOnScreen && Lines[line].WidthTree.Count >= offset;
        }

        public void RemoveSpan(int startLine, int startOffset, int endLine, int endOffset)
        {
            if (startLine == endLine) //One line selection
            {
                Lines[startLine].Chars.RemoveRange(startOffset, endOffset - startOffset);

                Caret.SetPosition(startOffset, startLine);
                Caret.EndSelection();
            }
            else //Multiline selection
            {
                Lines[startLine].Chars.RemoveRange(startOffset, Lines[startLine].Chars.Count - startOffset);
                Lines[endLine].Chars.RemoveRange(0, endOffset);
                Lines[startLine].Chars.AddRange(Lines[endLine].Chars);

                Lines.RemoveRange(startLine + 1, endLine - startLine);

                Caret.SetPosition(startOffset, startLine);
                Caret.EndSelection();
            }
        }

        public void TransformDrawingContext(ref DrawingContext drawingContext)
        {
            drawingContext.PushTransform(new TranslateTransform(HorizontalTransform, VerticalTransform));
        }

        public void SetVerticalOffset(double offset)
        {
            FirstVisibleLine = Math.Max(0, (int)(offset / LineHeight));
            VerticalTransform = -(offset % LineHeight);

            VerticalScrollBar.Value = offset;

            TextViewLayer.Repaint(true);
        }

        public void DrawMetrics(string text)
        {
            MetricsLayer.Repaint(text);
        }
        #endregion

        #region Properties
        public static DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset",
                                typeof(double),
                                typeof(Editor),
                                new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }

        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            Editor scrollViewer = target as Editor;

            if (scrollViewer != null)
            {
                scrollViewer.SetVerticalOffset((double)e.NewValue);
            }
        }
        #endregion

        #region Input handling
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            HandleInput(e.Text);
        }

        private void HandleInput(string text)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.V))
            {
                Selection? selection = Caret.GetCurentSelection();

                if (selection != null)
                {
                    RemoveSpan(selection.Value.StartLine, selection.Value.StartOffset, selection.Value.EndLine, selection.Value.EndOffset);
                }

                List<string> clipboardText = Clipboard.GetText().Split(Environment.NewLine).ToList();

                Lines[Caret.CurentLine].Chars.InsertRange(Caret.CurentOffset, clipboardText[0].ToCharArray().Select(c => c.ToString()).ToList());
                foreach (string s in clipboardText.GetRange(1, clipboardText.Count - 1))
                {
                    Lines.Add(new VirtualLine(this) { Chars = s.ToCharArray().Select(c => c.ToString()).ToList() });
                }

                Caret.SetPosition(Caret.CurentOffset + clipboardText[clipboardText.Count - 1].Length, Caret.CurentLine + clipboardText.Count - 1);
                Caret.EndSelection();

                //MessageBox.Show("Paste complete, document is now " + Lines.Count.ToString() + " lines long");
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.A))
            {
                Caret.SetSelection(0, 0, Lines[Lines.Count - 1].Chars.Count, Lines.Count - 1);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.C))
            {
                string result = "";

                Selection? selection = Caret.GetCurentSelection();

                if (selection == null)
                {
                    return;
                }

                for (int i = selection.Value.StartLine; i <= selection.Value.EndLine; i++)
                {
                    if(selection.Value.StartLine == selection.Value.EndLine)
                    {
                        result += string.Join("", Lines[i].Chars.GetRange(selection.Value.StartOffset, selection.Value.EndOffset - selection.Value.StartOffset));
                        break;
                    }

                    if (i == selection.Value.StartLine)
                    {
                        result += string.Join("", Lines[i].Chars.GetRange(Caret.SelectionStartOffset, Lines[i].Chars.Count - Caret.SelectionStartOffset)) + Environment.NewLine;
                    }
                    else if (i == selection.Value.EndLine)
                    {
                        result += string.Join("", Lines[i].Chars.GetRange(0, Caret.CurentOffset));
                    }
                    else
                    {
                        result += string.Join("", Lines[i]) + Environment.NewLine;
                    }
                }

                Clipboard.SetText(result);
            }
            else if (Keyboard.IsKeyDown(Key.Back))
            {
                Selection? selection = Caret.GetCurentSelection();

                if (selection != null)
                {
                    RemoveSpan(selection.Value.StartLine, selection.Value.StartOffset, selection.Value.EndLine, selection.Value.EndOffset);
                }
                else //No selection
                {
                    Lines[Caret.CurentLine].Chars.RemoveAt(Caret.CurentOffset - 1);
                    Caret.CurentOffset--;
                }
            }
            else
            {
                Selection? selection = Caret.GetCurentSelection();

                if (selection != null)
                {
                    RemoveSpan(selection.Value.StartLine, selection.Value.StartOffset, selection.Value.EndLine, selection.Value.EndOffset);
                }

                if (text == "\r\n" || text == "\r" || text == "\n")
                {
                    Lines.Insert(Caret.CurentLine + 1, new VirtualLine(this) { Chars = Lines[Caret.CurentLine].Chars.GetRange(Caret.CurentOffset, Lines[Caret.CurentLine].Chars.Count - Caret.CurentOffset) });
                    Lines[Caret.CurentLine].Chars.RemoveRange(Caret.CurentOffset, Lines[Caret.CurentLine].Chars.Count - Caret.CurentOffset);

                    Caret.EndSelection();

                    Caret.SetPosition(0, Caret.CurentLine + 1);
                }
                else
                {
                    Lines[Caret.CurentLine].Chars.Insert(Caret.CurentOffset, text);

                    Caret.EndSelection();

                    Caret.SetPosition(Caret.CurentOffset + 1, Caret.CurentLine);
                }
            }

            VerticalScrollBar.Maximum = (Lines.Count - LinesOnScreen) * LineHeight;

            TextViewLayer.Repaint(false);
        }
        #endregion

        #region Resizing
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LinesOnScreen = (int)(this.ActualHeight / LineHeight) + 2;
            VerticalScrollBar.Maximum = (Lines.Count - LinesOnScreen) * LineHeight;

            FirstVisibleLine = Math.Max(0, Math.Min(Lines.Count - LinesOnScreen, FirstVisibleLine));

            TextViewLayer.Repaint(true);
        }
        #endregion

        #region Selection and caret movement
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsFocused)
            {
                this.Focus();
            }

            IgnoreMouseUp = false;

            Point mousePos = Mouse.GetPosition(this);

            DocumentPosition position = DocumentPositionFromScreenPosition(mousePos.X, mousePos.Y);

            Caret.BeginSelection(position.Offset, position.Line);
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (Caret.MainCaretState == Caret.CaretState.SelectionActive)
            {
                Point mousePos = Mouse.GetPosition(this);

                DocumentPosition position = DocumentPositionFromScreenPosition(mousePos.X, mousePos.Y);

                Caret.SetPosition(position.Offset, position.Line);
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IgnoreMouseUp)
            {
                Caret.EndSelection();
            }
        }

        private bool IgnoreMouseUp = false;
        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            IgnoreMouseUp = true;

            e.Handled = true;

            Point mousePos = Mouse.GetPosition(this);

            DocumentPosition position = DocumentPositionFromScreenPosition(mousePos.X, mousePos.Y);

            string curentLine = Lines[position.Line].ToString();

            Debug.WriteLine(position.Offset + " " + curentLine.Length);

            int start = curentLine.LastIndexOfAny(@" ,.()[]{};:''/\|?<>*&%$#@".ToCharArray(), Math.Min(position.Offset, curentLine.Length - 1));
            int end = curentLine.IndexOfAny(@" ,.()[]{};:''/\|?<>*&%$#@".ToCharArray(), Math.Max(0, Math.Min(position.Offset, curentLine.Length - 1)));

            start = start == -1 ? 0 : start + 1;
            end = end == -1 ? curentLine.Length : end;

            Debug.WriteLine(start + " " + end);

            Caret.SetSelection(start, position.Line, end, position.Line);
        }
        #endregion

        #region Scrolling
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (AnimateScroll)
            {
                IsAnimating = true;

                if (e.Delta < 0)
                {
                    ActualVerticalOffset = Math.Min(ActualVerticalOffset + LinesToScroll * LineHeight, Math.Max(0, (Lines.Count - LinesOnScreen) * LineHeight));

                    ScrollingHelper.AnimateScroll(this, ActualVerticalOffset, Math.Min(Math.Max(Math.Abs(ActualVerticalOffset - VerticalOffset) * ScrollInertia, MinScrollAnimationSpeed), MaxScrollAnimationSpeed), 0.1);
                }
                else if (e.Delta > 0)
                {
                    ActualVerticalOffset = Math.Max(0, ActualVerticalOffset - LinesToScroll * LineHeight);

                    ScrollingHelper.AnimateScroll(this, ActualVerticalOffset, Math.Min(Math.Max(Math.Abs(ActualVerticalOffset - VerticalOffset) * ScrollInertia, MinScrollAnimationSpeed), MaxScrollAnimationSpeed), 0.1);
                }
            }
            else
            {
                if (e.Delta < 0)
                {
                    VerticalOffset = Math.Min(VerticalOffset + LinesToScroll * LineHeight, Math.Max(0, (Lines.Count - LinesOnScreen) * LineHeight));
                }
                else if (e.Delta > 0)
                {
                    VerticalOffset = Math.Max(0, VerticalOffset - LinesToScroll * LineHeight);
                }
            }
        }

        private void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VerticalOffset != e.NewValue)
            {
                if (IsAnimating)
                {
                    BeginAnimation(VerticalOffsetProperty, null);
                    IsAnimating = false;
                }

                VerticalOffset = e.NewValue;
                ActualVerticalOffset = VerticalOffset;
            }
        }
        #endregion

        #region Rendering
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Background, new Pen(Brushes.Black, 0), new Rect(0, 0, ActualWidth, ActualHeight));
        }
        #endregion
    }
}
