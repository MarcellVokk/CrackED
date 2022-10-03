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
        public Brush Foreground { get; set; }

        public StyleSpan(int start, int lenght, Brush foreground)
        {
            Start = start;
            Lenght = lenght;
            Foreground = foreground;
        }
    }
}
