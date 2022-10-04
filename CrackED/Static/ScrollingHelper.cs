using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace CrackED
{
    public static class ScrollingHelper
    {
		public static void AnimateScroll(Editor editor, double ToValue, double time = 200, double acceleration = 1)
		{
			DoubleAnimationUsingKeyFrames keyFramesAnimation = new DoubleAnimationUsingKeyFrames() { AccelerationRatio = acceleration, FillBehavior = FillBehavior.HoldEnd, Duration = TimeSpan.FromMilliseconds(time) };

			keyFramesAnimation.KeyFrames.Add(
				new SplineDoubleKeyFrame(
				ToValue,
				KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(time)),
				new KeySpline(0.5, 1, 0.5, 1)));

			keyFramesAnimation.SetValue(Timeline.DesiredFrameRateProperty, 120);

			editor.BeginAnimation(Editor.VerticalOffsetProperty, keyFramesAnimation, HandoffBehavior.Compose);
		}
	}
}
