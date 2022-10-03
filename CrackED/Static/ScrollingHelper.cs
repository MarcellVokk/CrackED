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
		private static DoubleAnimationUsingKeyFrames LastAppliedAnimation;
		private static EventHandler LastAppliedAnimation_onCompleted;

		public static void AnimateScroll(Editor editor, double ToValue, EventHandler onCompleted = null, double time = 200, double acceleration = 1)
		{
			if (LastAppliedAnimation != null && LastAppliedAnimation_onCompleted != null)
			{
				LastAppliedAnimation.Completed -= LastAppliedAnimation_onCompleted;
			}

			DoubleAnimationUsingKeyFrames keyFramesAnimation = new DoubleAnimationUsingKeyFrames() { AccelerationRatio = acceleration };
			keyFramesAnimation.FillBehavior = FillBehavior.HoldEnd;
			keyFramesAnimation.Duration = TimeSpan.FromMilliseconds(time);
			keyFramesAnimation.KeyFrames.Add(
				new SplineDoubleKeyFrame(
					ToValue,
					KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(time)),
					new KeySpline(0.5, 1.0, 0.5, 1.0)
					)
				);

			if (onCompleted != null)
			{
				keyFramesAnimation.Completed += onCompleted;
			}

			LastAppliedAnimation = keyFramesAnimation;
			LastAppliedAnimation_onCompleted = onCompleted;

			editor.BeginAnimation(Editor.VerticalOffsetProperty, keyFramesAnimation, HandoffBehavior.Compose);
		}
	}
}
