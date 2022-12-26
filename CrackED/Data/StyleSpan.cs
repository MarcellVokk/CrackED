using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CrackED
{
    public class StyleSpan
    {
        public int Start { get; set; } = 0;
        public int Lenght { get; set; } = 0;
        public SolidColorBrush Foreground { get; set; }
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;

        public StyleSpan(int start, int lenght, SolidColorBrush color, bool isBold = false, bool isItalic = false)
        {
            Start = start;
            Lenght = lenght;
            Foreground = color;
            IsBold = isBold;
            IsItalic = isItalic;
        }
    }
}
