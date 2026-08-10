using System.Collections;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Extension point for window open/close animation. BaseWindow looks for this
    /// component on the same GameObject; if it's not there, it falls back to a
    /// simple built-in fade+scale (see BaseWindow.DefaultAnimateRoutine).
    ///
    /// This is the seam the Pro package plugs into: an "AdvancedWindowAnimator"
    /// (Ease dropdown, DOTween Sequence, per-phase timing) can be dropped on top
    /// of an existing window prefab as a second component, with zero changes to
    /// the Free scripts or existing prefabs.
    /// </summary>
    public interface IWindowAnimator
    {
        /// <summary>Play the open animation. Must leave CanvasGroup.alpha at 1 and localScale at Vector3.one when done.</summary>
        IEnumerator PlayOpen(RectTransform rect, CanvasGroup canvasGroup);

        /// <summary>Play the close animation. Must leave CanvasGroup.alpha at 0 when done.</summary>
        IEnumerator PlayClose(RectTransform rect, CanvasGroup canvasGroup);
    }
}
