using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace CrackED
{
    /// <summary>
    /// Interaction logic for BackgroundLayer.xaml
    /// </summary>
    public partial class BackgroundLayer : UserControl
    {
        internal Editor Owner;

        internal TextSearchManagger TextSearchManagger;

        public BackgroundLayer(Editor owner)
        {
            InitializeComponent();

            Owner = owner;

            TextSearchManagger = new TextSearchManagger(this);
        }

        public void Repaint()
        {
            RepaintRequested = true;
            InvalidateVisual();
        }

        private bool RepaintRequested = false;
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (RepaintRequested)
            {
                RepaintRequested = false;

                Owner.TransformDrawingContext(ref drawingContext);

                Stopwatch sw = Stopwatch.StartNew();

                Debug.WriteLine("Render: BackgroundLayer");

                TextSearchManagger.Draw(ref drawingContext);

                sw.Stop();
            }
        }
    }
}
