using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CrackED
{
    public static class ColorHelper
    {
        public static HSVColor RGBToHSV(int r, int g, int b)
        {
            double delta, min;
            double h = 0, s, v;

            min = Math.Min(Math.Min(r, g), b);
            v = Math.Max(Math.Max(r, g), b);
            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;

            else
            {
                if (r == v)
                    h = (g - b) / delta;
                else if (g == v)
                    h = 2 + (b - r) / delta;
                else if (b == v)
                    h = 4 + (r - g) / delta;

                h *= 60;

                if (h < 0.0)
                    h = h + 360;
            }

            return new HSVColor(h, s * 100, (v / 255) * 100);
        }

        public static Color HSVToRGB(int h, int s, int v)
        {
            var rgb = new int[3];

            var baseColor = (h + 60) % 360 / 120;
            var shift = (h + 60) % 360 - (120 * baseColor + 60);
            var secondaryColor = (baseColor + (shift >= 0 ? 1 : -1) + 3) % 3;

            //Setting Hue
            rgb[baseColor] = 255;
            rgb[secondaryColor] = (int)((MathF.Abs(shift) / 60.0f) * 255.0f);

            //Setting Saturation
            for (var i = 0; i < 3; i++)
                rgb[i] += (int)((255 - rgb[i]) * ((100 - s) / 100.0f));

            //Setting Value
            for (var i = 0; i < 3; i++)
                rgb[i] -= (int)(rgb[i] * (100 - v) / 100.0f);

            return Color.FromArgb(255, (byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
        }

        public static Color HSVToRGB(HSVColor color)
        {
            return HSVToRGB(color.hue, color.saturation, color.value);
        }
    }

    public class HSVColor
    {
        public HSVColor(double hue, double saturation, double value)
        {
            this.hue = (int)Math.Round(hue, 0);
            this.saturation = (int)Math.Round(saturation, 0);
            this.value = (int)Math.Round(value, 0);
        }

        public HSVColor()
        {

        }

        public int hue { get; set; }
        public int saturation { get; set; }
        public int value { get; set; }
    }
}
