//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Media;

//namespace CrackED
//{
//    public class Span
//    {
//        public int Start { get; set; } = 0;
//        public int Lenght { get; set; } = 0;
//        public Color Color { get; set; }
//        public bool IsBold { get; set; } = false;
//        public bool IsItalic { get; set; } = false;

//        public Span(int start, int lenght, Color color, bool isBold = false, bool isItalic = false)
//        {
//            Start = start;
//            Lenght = lenght;
//            Color = color;
//            IsBold = isBold;
//            IsItalic = isItalic;
//        }

//        public bool CompareStyle(Span compareWith)
//        {
//            return compareWith.IsBold == compareWith.IsBold && compareWith.IsItalic == compareWith.IsItalic;
//        }

//        public StyleSpan ToRuntimeSpan()
//        {
//            return new StyleSpan(Start, Lenght, IsBold, IsItalic);
//        }
//    }
//}
