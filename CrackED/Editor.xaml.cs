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
        internal int FirstRenderedLine = 0;
        internal int LinesToRender = 0;
        internal int ExtraLines = 2;

        private bool IsAnimating = false;

        private double ActualVerticalOffset = 0d;

        internal double VerticalTransform = 0d;
        internal double HorizontalTransform = 0d;

        internal GlyphTypeface Typeface_Regular;
        internal GlyphTypeface Typeface_RegularItalic;
        internal GlyphTypeface Typeface_Bold;
        internal GlyphTypeface Typeface_BoldItalic;

        internal SelectionLayer SelectionLayer;
        internal TextViewLayer TextViewLayer;
        internal CaretLayer CaretLayer;
        internal BackgroundLayer BackgroundLayer;
        internal MetricsLayer MetricsLayer;

        public List<VirtualLine> Lines = new List<VirtualLine>();
        #endregion

        #region Public fields
        public Caret Caret;

        public new double FontSize = 14;
        public new FontFamily FontFamily = new FontFamily("Consolas");
        public new Brush Foreground = new SolidColorBrush(Color.FromArgb(255, 219, 219, 228));
        public new Brush Background = new SolidColorBrush(Color.FromArgb(255, 38, 38, 38));

        public double LineHeight = 24;
        public double VerticalTextOffset = -3;

        public double MinScrollAnimationSpeed = 360d;
        public double MaxScrollAnimationSpeed = 720d;
        public double LinesToScroll = 3;
        public double ScrollInertia = 1.3d;
        public bool ImmediateRendering { get; set; } = false;
        public bool AnimateScroll { get; set; } = true;
        public bool SmoothScrollBar = true;
        public bool ScrollBeyondLastLine = false;

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
            Lines[line].AddSpan(styleSpan);
        }

        public void ClearStyleSpans(int line)
        {
            Lines[line].ClearSpans();
        }

        public void ClearStyleSpans()
        {
            for(int i = 0; i < Lines.Count; i++)
            {
                Lines[i].ClearSpans();
            }
        }

        public void UpdateOptions()
        {
            Caret.Caret_blink.Interval = CaretBlinkInterval;
            Caret.Caret_blink_timeout.Interval = CaretBlinkTimeout;

            //VerticalOffset = (double)GetValue(VerticalOffsetProperty);
            ActualVerticalOffset = VerticalOffset;

            if (IsAnimating)
            {
                //BeginAnimation(VerticalOffsetProperty, null);
                IsAnimating = false;
            }

            TextViewLayer.ReInitialize();

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

            if (!(new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal)).TryGetGlyphTypeface(out Typeface_Regular))
            {
                Debug.WriteLine("WARNING: Could not load regular typeface");
            }

            if (!(new Typeface(FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal)).TryGetGlyphTypeface(out Typeface_RegularItalic))
            {
                Debug.WriteLine("WARNING: Could not load regular_italic typeface");
            }

            if (!(new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal)).TryGetGlyphTypeface(out Typeface_Bold))
            {
                Debug.WriteLine("WARNING: Could not load bold typeface");
            }

            if (!(new Typeface(FontFamily, FontStyles.Italic, FontWeights.Bold, FontStretches.Normal)).TryGetGlyphTypeface(out Typeface_BoldItalic))
            {
                Debug.WriteLine("WARNING: Could not load bold_italic typeface");
            }
        }
        #endregion

        #region Helpers

        public DocumentPosition DocumentPositionFromScreenPosition(double x, double y)
        {
            y -= VerticalTransform;
            x -= HorizontalTransform;

            int line = Math.Min((int)(y / LineHeight) + FirstRenderedLine, Lines.Count - 1);

            int offset = 0;
            double screenOffset = 0;

            while (offset < Lines[line].Content.TextLenght)
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
            return line >= FirstRenderedLine && line < FirstRenderedLine + LinesToRender && (Lines[line].WidthTree.Count > offset);
        }

        public void RemoveSpan(int startLine, int startOffset, int endLine, int endOffset)
        {
            if (startLine == endLine) //One line selection
            {
                Lines[startLine].Content.RemoveRange(startOffset, endOffset - startOffset);

                Caret.SetPosition(startOffset, startLine);
                Caret.EndSelection();
            }
            else //Multiline selection
            {
                Lines[startLine].Content.RemoveRange(startOffset, Lines[startLine].Content.TextLenght - startOffset);
                Lines[endLine].Content.RemoveRange(0, endOffset);
                Lines[startLine].Content.Apend(Lines[endLine].Content);

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
            if (IsAnimating)
            {
                BeginAnimation(VerticalOffsetProperty, null);
                IsAnimating = false;
            }

            VerticalOffset = offset;
            ActualVerticalOffset = offset;
        }

        public void SetHorizontalOffset(double offset)
        {
            HorizontalTransform = -offset;

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
            private set { SetValue(VerticalOffsetProperty, value); }
        }

        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            Editor editor = target as Editor;
            double offset = (double)e.NewValue;

            if (editor != null)
            {
                if(offset / editor.LineHeight <= editor.Lines.Count - editor.LinesToRender)
                {
                    editor.FirstRenderedLine = Math.Max(0, (int)(offset / editor.LineHeight));
                    editor.VerticalTransform = -(offset % editor.LineHeight);
                }
                else
                {
                    editor.FirstRenderedLine = Math.Max(0, editor.Lines.Count - editor.LinesToRender);
                    editor.VerticalTransform = -(offset - (Math.Max(0, editor.Lines.Count - editor.LinesToRender)) * editor.LineHeight);
                }


                if (editor.VerticalScrollBar.Value != offset)
                {
                    editor.VerticalScrollBar.Value = offset;
                }

                editor.TextViewLayer.Repaint(true);
            }
        }

        public double HorizontalOffset { get; private set; }
        #endregion

        #region Input handling
        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            HandleTextInput(e.Text);
            CalculateMaxVOffset();
            TextViewLayer.Repaint(false);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = HandleKeyInput();
            CalculateMaxVOffset();
            TextViewLayer.Repaint(false);
        }

        private void HandleTextInput(string text)
        {
            Selection? selection = Caret.GetCurentSelection();

            if (selection != null)
            {
                RemoveSpan(selection.Value.StartLine, selection.Value.StartOffset, selection.Value.EndLine, selection.Value.EndOffset);
            }

            if (text == "\r\n" || text == "\r" || text == "\n")
            {
                Lines.Insert(Caret.CurentLine + 1, new VirtualLine(this) { Content = new LineContent(Lines[Caret.CurentLine].Content.GetRange(Caret.CurentOffset, Lines[Caret.CurentLine].Content.TextLenght - Caret.CurentOffset)) });
                Lines[Caret.CurentLine].Content.RemoveRange(Caret.CurentOffset, Lines[Caret.CurentLine].Content.TextLenght - Caret.CurentOffset);

                Caret.EndSelection();

                Caret.SetPosition(0, Caret.CurentLine + 1);
            }
            else
            {
                Lines[Caret.CurentLine].Content.Insert(Caret.CurentOffset, text);

                CalculateMaxHOffset(Caret.CurentLine);

                Caret.EndSelection();

                Caret.SetPosition(Caret.CurentOffset + 1, Caret.CurentLine);
            }
        }

        private bool HandleKeyInput()
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.V))
            {
                Selection? selection = Caret.GetCurentSelection();

                if (selection != null)
                {
                    RemoveSpan(selection.Value.StartLine, selection.Value.StartOffset, selection.Value.EndLine, selection.Value.EndOffset);
                }

                List<string> clipboardText = Clipboard.GetText().Split(Environment.NewLine).ToList();

                int curentLine = Caret.CurentLine;
                int curentOffset = Caret.CurentOffset;

                List<string> apendToLastLine = Lines[curentLine].Content.GetRange(Caret.CurentOffset, Lines[curentLine].Content.TextLenght - Caret.CurentOffset);

                Lines[curentLine].Content.RemoveRange(Caret.CurentOffset, Lines[curentLine].Content.TextLenght - Caret.CurentOffset);
                Lines[curentLine].Content.AddRange(clipboardText[0].ToCharArray().Select(c => c.ToString()).ToList());
                CalculateMaxHOffset(curentLine);

                for (int i = 1; i < clipboardText.Count; i++)
                {
                    curentLine++;
                    curentOffset = 0;

                    Lines.Insert(curentLine, new VirtualLine(this) { Content = new LineContent(clipboardText[i].ToCharArray().Select(c => c.ToString()).ToList()) });
                    CalculateMaxHOffset(curentLine);
                }

                Lines[curentLine].Content.AddRange(apendToLastLine);
                CalculateMaxHOffset(curentLine);

                Caret.SetPosition(curentOffset + clipboardText.LastOrDefault("").Length, curentLine);
                Caret.EndSelection();
                Caret.ScrollIntoView();

                return true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.A))
            {
                Caret.SetSelection(0, 0, Lines[Lines.Count - 1].Content.TextLenght, Lines.Count - 1);

                return true;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.IsKeyDown(Key.C))
            {
                string result = "";

                Selection? selection = Caret.GetCurentSelection();

                if (selection == null)
                {
                    return true;
                }

                for (int i = selection.Value.StartLine; i <= selection.Value.EndLine; i++)
                {
                    if (selection.Value.StartLine == selection.Value.EndLine)
                    {
                        result += string.Join("", Lines[i].Content.GetRange(selection.Value.StartOffset, selection.Value.EndOffset - selection.Value.StartOffset));
                        break;
                    }

                    if (i == selection.Value.StartLine)
                    {
                        result += string.Join("", Lines[i].Content.GetRange(selection.Value.StartOffset, Lines[i].Content.TextLenght - selection.Value.StartOffset)) + Environment.NewLine;
                    }
                    else if (i == selection.Value.EndLine)
                    {
                        result += string.Join("", Lines[i].Content.GetRange(0, Caret.CurentOffset));
                    }
                    else
                    {
                        result += string.Join("", Lines[i].Content.ToText()) + Environment.NewLine;
                    }
                }

                Clipboard.SetText(result);

                return true;
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
                    if(Caret.CurentOffset - 1 >= 0)
                    {
                        Lines[Caret.CurentLine].Content.RemoveAt(Caret.CurentOffset - 1);

                        Caret.SetPosition(Caret.CurentOffset - 1, Caret.CurentLine);
                    }
                }

                return true;
            }
            else if (Keyboard.IsKeyDown(Key.Left))
            {
                if (Caret.CurentOffset - 1 < 0 && Caret.CurentLine > 0)
                {
                    Caret.SetPosition(Lines[Caret.CurentLine - 1].Content.TextLenght, Caret.CurentLine - 1);
                }
                else if (Caret.CurentOffset - 1 >= 0)
                {
                    Caret.SetPosition(Caret.CurentOffset - 1, Caret.CurentLine);
                }

                return true;
            }
            else if (Keyboard.IsKeyDown(Key.Right))
            {
                if (Caret.CurentOffset + 1 > Lines[Caret.CurentLine].Content.TextLenght && Caret.CurentLine < Lines.Count - 1)
                {
                    Caret.SetPosition(0, Caret.CurentLine + 1);
                }
                else if (Caret.CurentOffset + 1 <= Lines[Caret.CurentLine].Content.TextLenght)
                {
                    Caret.SetPosition(Caret.CurentOffset + 1, Caret.CurentLine);
                }

                return true;
            }
            else if (Keyboard.IsKeyDown(Key.Up))
            {
                Caret.SetPosition(Caret.UnclampedOffset, Caret.CurentLine - 1);
                Caret.ScrollIntoView();

                return true;
            }
            else if (Keyboard.IsKeyDown(Key.Down))
            {
                Caret.SetPosition(Caret.UnclampedOffset, Caret.CurentLine + 1);
                Caret.ScrollIntoView();

                return true;
            }

            return false;
        }

        private void CalculateMaxVOffset()
        {
            VerticalScrollBar.Maximum = Lines.Count * LineHeight - (ScrollBeyondLastLine ? LineHeight : RenderArea.ActualHeight);
            VerticalOffset = Math.Max(0, VerticalOffset);
        }

        private double MaxHOffset = 0;
        private void CalculateMaxHOffset(int line)
        {
            double width = 0;

            var query = Lines[line].Content.CountDistinct();

            foreach (var item in query)
            {
                ushort glyphIndex = Typeface_Regular.CharacterToGlyphMap['?'];
                if (Typeface_Regular.CharacterToGlyphMap.ContainsKey(item.Text[0]))
                {
                    glyphIndex = Typeface_Regular.CharacterToGlyphMap[item.Text[0]];
                }

                width += Typeface_Regular.AdvanceWidths[glyphIndex] * FontSize * item.Count;
            }

            if (width > HorizontalScrollBar.Maximum)
            {
                MaxHOffset = width;
                HorizontalScrollBar.Maximum = MaxHOffset - this.RenderArea.ActualWidth * 0.5;
            }
        }
        #endregion

        #region Resizing
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LinesToRender = (int)(RenderArea.ActualHeight / LineHeight) + ExtraLines;
            CalculateMaxVOffset();
            HorizontalScrollBar.Maximum = MaxHOffset - this.RenderArea.ActualWidth * 0.5;

            SetVerticalOffset(Math.Max(0, Math.Min(VerticalOffset, VerticalScrollBar.Maximum)));

            TextViewLayer.Repaint(true);
        }
        #endregion

        #region Selection and caret movement
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(this);

            DocumentPosition position = DocumentPositionFromScreenPosition(mousePos.X, mousePos.Y);

            if (!IsFocused)
            {
                this.Focus();
            }

            IgnoreMouseUp = false;

            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                Caret.BeginSelection(Caret.CurentOffset, Caret.CurentLine);

                Caret.SetPosition(position.Offset, position.Line);
            }
            else
            {
                Caret.BeginSelection(position.Offset, position.Line);
            }
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

            string curentLine = Lines[position.Line].Content.ToText();

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
                    ActualVerticalOffset = Math.Min(VerticalScrollBar.Maximum, ActualVerticalOffset + LinesToScroll * LineHeight);

                    ScrollingHelper.AnimateScroll(this, ActualVerticalOffset, Math.Min(Math.Max(Math.Abs(ActualVerticalOffset - VerticalOffset) * ScrollInertia, MinScrollAnimationSpeed), MaxScrollAnimationSpeed), 0.1);
                }
                else if (e.Delta > 0)
                {
                    ActualVerticalOffset = Math.Max(VerticalScrollBar.Minimum, ActualVerticalOffset - LinesToScroll * LineHeight);

                    ScrollingHelper.AnimateScroll(this, ActualVerticalOffset, Math.Min(Math.Max(Math.Abs(ActualVerticalOffset - VerticalOffset) * ScrollInertia, MinScrollAnimationSpeed), MaxScrollAnimationSpeed), 0.1);
                }
            }
            else
            {
                if (e.Delta < 0)
                {
                    VerticalOffset = Math.Min(VerticalScrollBar.Maximum, VerticalOffset + LinesToScroll * LineHeight);
                }
                else if (e.Delta > 0)
                {
                    VerticalOffset = Math.Max(VerticalScrollBar.Minimum, VerticalOffset - LinesToScroll * LineHeight);
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

                if (SmoothScrollBar)
                {
                    VerticalOffset = e.NewValue;
                    ActualVerticalOffset = VerticalOffset;
                }
                else
                {
                    VerticalOffset = (int)(e.NewValue / LineHeight) * LineHeight;
                    ActualVerticalOffset = VerticalOffset;
                }
            }
        }

        private void HorizontalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetHorizontalOffset(HorizontalScrollBar.Value);
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
