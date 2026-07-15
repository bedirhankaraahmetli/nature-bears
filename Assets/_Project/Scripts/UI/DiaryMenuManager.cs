using System;
using System.Collections;
using System.Collections.Generic;
using NatureBears.Audio;
using UnityEngine;

namespace NatureBears.UI
{
    /// <summary>
    /// Diegetic "Scout Diaries" — full-screen submenus (Build, Research,
    /// Manage) that fade in/out like pages of a field journal.
    ///
    /// Deliberately lightweight for mobile: plain coroutines lerping
    /// CanvasGroup.alpha (no Animator, no tween plugin). interactable /
    /// blocksRaycasts flip at the correct edges so a fading page never eats
    /// touches, and hidden pages are SetActive(false) so they cost nothing.
    ///
    /// Buttons wire to <see cref="OpenDiary(string)"/> / <see cref="CloseCurrentDiary"/>
    /// in the inspector (uGUI OnClick with a string argument). Only one diary
    /// is open at a time — opening another cross-fades to it.
    /// </summary>
    public class DiaryMenuManager : MonoBehaviour
    {
        [Serializable]
        private struct DiaryPage
        {
            [Tooltip("Id used by buttons, e.g. \"Build\", \"Research\", \"Manage\".")]
            public string id;
            [Tooltip("Full-screen panel root. Needs a CanvasGroup; may start inactive.")]
            public CanvasGroup canvasGroup;
        }

        public static DiaryMenuManager Instance { get; private set; }

        [Header("Pages")]
        [SerializeField] private DiaryPage[] pages;

        [Header("Transition")]
        [Tooltip("Fade duration per page, seconds. Uses unscaled time.")]
        [SerializeField] private float fadeSeconds = 0.25f;

        [Header("Audio (optional)")]
        [Tooltip("Page-turn sound on open/close. Falls back to the AudioManager's default UI click when empty.")]
        [SerializeField] private AudioClip pageTurnClip;

        private readonly Dictionary<string, CanvasGroup> _pageById =
            new Dictionary<string, CanvasGroup>(4, StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<CanvasGroup, Coroutine> _activeFades =
            new Dictionary<CanvasGroup, Coroutine>(4);

        private CanvasGroup _openPage;

        /// <summary>True while any diary page is open (or fading in).</summary>
        public bool IsAnyDiaryOpen => _openPage != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // scene-owned: no DontDestroyOnLoad

            if (pages != null)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    if (string.IsNullOrEmpty(pages[i].id) || pages[i].canvasGroup == null) continue;

                    _pageById[pages[i].id] = pages[i].canvasGroup;
                    SnapHidden(pages[i].canvasGroup);
                }
            }
        }

        // ------------------------------------------------------------------
        // Public API (inspector-wired buttons)
        // ------------------------------------------------------------------

        /// <summary>Opens the diary with the given id, cross-fading away whatever is open.</summary>
        public void OpenDiary(string id)
        {
            if (string.IsNullOrEmpty(id) || !_pageById.TryGetValue(id, out CanvasGroup page))
            {
                Debug.LogWarning($"[DiaryMenuManager] Unknown diary id '{id}'.");
                return;
            }

            if (page == _openPage) return;

            PlayPageTurn();

            if (_openPage != null)
                StartFade(_openPage, false);

            _openPage = page;
            StartFade(page, true);
        }

        /// <summary>Closes whichever diary is open (back/close buttons all wire here).</summary>
        public void CloseCurrentDiary()
        {
            if (_openPage == null) return;

            PlayPageTurn();
            StartFade(_openPage, false);
            _openPage = null;
        }

        /// <summary>Opens the diary if closed, closes it if it is the one open — for HUD toggle buttons.</summary>
        public void ToggleDiary(string id)
        {
            if (_openPage != null && _pageById.TryGetValue(id, out CanvasGroup page) && page == _openPage)
                CloseCurrentDiary();
            else
                OpenDiary(id);
        }

        // ------------------------------------------------------------------
        // Fade machinery
        // ------------------------------------------------------------------

        private void StartFade(CanvasGroup page, bool visible)
        {
            // Cancel an in-flight fade on this page so rapid open/close never
            // leaves two coroutines fighting over the same alpha.
            if (_activeFades.TryGetValue(page, out Coroutine running) && running != null)
                StopCoroutine(running);

            _activeFades[page] = StartCoroutine(FadeRoutine(page, visible));
        }

        private IEnumerator FadeRoutine(CanvasGroup page, bool visible)
        {
            // A page must never eat touches while mid-fade; interactable only
            // turns on once the fade-in fully lands.
            page.interactable = false;
            page.blocksRaycasts = visible;

            if (visible)
                page.gameObject.SetActive(true);

            float from = page.alpha;
            float to = visible ? 1f : 0f;
            float duration = Mathf.Max(0.01f, fadeSeconds) * Mathf.Abs(to - from);

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                page.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            page.alpha = to;
            page.interactable = visible;

            if (!visible)
                page.gameObject.SetActive(false);

            _activeFades[page] = null;
        }

        private static void SnapHidden(CanvasGroup page)
        {
            page.alpha = 0f;
            page.interactable = false;
            page.blocksRaycasts = false;
            page.gameObject.SetActive(false);
        }

        private void PlayPageTurn()
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null) return;

            if (pageTurnClip != null)
                audio.PlaySound(pageTurnClip);
            else
                audio.PlayUIClick();
        }
    }
}
