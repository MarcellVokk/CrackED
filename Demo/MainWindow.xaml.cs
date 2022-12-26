using CrackED;
using System;
using System.Collections.Generic;
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

namespace Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //Smoothscrolling with better performance, but also uses more CPU
            Editor.AnimateScroll = true;
            Editor.ImmediateRendering = true;
            Editor.UpdateOptions();

            //Smoothscrolling with reduced performance, but also less CPU usage
            //Editor.AnimateScroll = true;
            //Editor.ImmediateRendering = false;
            //Editor.UpdateOptions();

            //Smoothscrolling off
            //Editor.AnimateScroll = false;
            //Editor.ImmediateRendering = true;
            //Editor.UpdateOptions();

            Editor.SerachForAll("a");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach(CrackED.VirtualLine l in Editor.Lines)
            {
                l.ClearSpans();

                int offset = 0;

                Random r = new Random();

                while (l.Content.ToText().IndexOfAny(@" ,.()[]{};:''/\|?<>*&%$#@".ToCharArray(), offset) != -1)
                {
                    offset = l.Content.ToText().IndexOfAny(@" ,.()[]{};:''/\|?<>*&%$#@".ToCharArray(), offset);
                    int endOffset = l.Content.ToText().IndexOfAny(@" ,.()[]{};:''/\|?<>*&%$#@".ToCharArray(), offset + 1);

                    l.AddSpan(new StyleSpan(offset, endOffset == -1 ? -1 : endOffset - offset, new SolidColorBrush(ColorHelper.HSVToRGB(new HSVColor(r.Next(0, 255), 100, 100)))));

                    if (endOffset == -1)
                    {
                        break;
                    }
                    else
                    {
                        offset = endOffset;
                    }
                }
            }
        }
    }
}