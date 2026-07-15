using NatureBears.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NatureBears.UI
{
    /// <summary>
    /// Attach to ANY clickable UI element (Button, toggle, tap zone) and it
    /// plays a soft click through the AudioManager's SFX channel on press.
    ///
    /// Uses IPointerClickHandler instead of Button.onClick so it works on
    /// every raycast target, requires zero inspector wiring, and keeps UI
    /// prefabs free of direct AudioManager references (null-guarded — silent
    /// until an AudioManager exists in the scene).
    /// </summary>
    public class UIButtonSound : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Optional override (e.g. page-turn). Empty = AudioManager's default UI click.")]
        [SerializeField] private AudioClip overrideClip;

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null) return;

            if (overrideClip != null)
                audio.PlaySound(overrideClip);
            else
                audio.PlayUIClick();
        }
    }
}
