using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroSequenceController : MonoBehaviour
{
    [Header("Scene to load when Play is pressed")]
    public string mapPlacementSceneName = "MapPlacement";
    public float exitFadeDuration = 1f; // fade to black + music fade out before the scene loads

    [Header("Intro camera - dollies back after the black fades out")]
    public Transform introCamera;
    public float cameraPullBackDistance = 1.5f;
    public float cameraPullBackDuration = 8f; // roughly the length of fade + lights + music + menu

    [Header("Black loading overlay - starts fully opaque, covers everything")]
    public Image blackOverlay;
    public float initialBlackHoldDuration = 1f;
    public float blackFadeOutDuration = 2f;

    [Header("Store interior lights - assign every light fixture in the scene")]
    public Light[] storeLights;
    public float lightsFadeInDuration = 3f;
    public float delayBetweenLights = 0.3f; // stagger each light turning on - set to 0 for all-at-once

    [Header("Background music - assign a clip, leave Play On Awake OFF")]
    public AudioSource musicSource;
    public float musicFadeInDuration = 2f;

    [Header("Start menu UI - the actual title/buttons, starts invisible and non-interactive")]
    public CanvasGroup menuCanvasGroup;
    public float menuFadeInDuration = 1.5f;

    Dictionary<Light, float> originalIntensities = new();
    bool isLeaving = false; // blocks double-clicking Play while the exit fade is running
    Vector3 cameraRestPosition;

    void Start()
    {
        // Capture each light's designed intensity BEFORE zeroing it out, so we know what to fade back up to.
        foreach (var light in storeLights)
        {
            if (light == null) continue;
            originalIntensities[light] = light.intensity;
            light.intensity = 0f;
        }

        SetImageAlpha(blackOverlay, 1f);
        blackOverlay.raycastTarget = true; // blocks clicks while the intro plays
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }
        if (musicSource != null) musicSource.volume = 0f;

        if (introCamera != null)
        {
            // The camera's Editor position is the final resting framing. Start pushed in, then dolly back to it.
            cameraRestPosition = introCamera.position;
            introCamera.position = cameraRestPosition + introCamera.forward * cameraPullBackDistance;
        }

        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(initialBlackHoldDuration);

        if (introCamera != null) StartCoroutine(PullBackCamera());

        yield return FadeImage(blackOverlay, 1f, 0f, blackFadeOutDuration);
        // IMPORTANT: the overlay is a later sibling than MenuGroup, so even at alpha 0 it would still
        // swallow every click meant for the buttons underneath. Turn its raycasting off once invisible.
        blackOverlay.raycastTarget = false;

        yield return StartCoroutine(FadeInLights());

        if (musicSource != null)
        {
            musicSource.Play();
            yield return FadeAudioVolume(musicSource, 0f, 1f, musicFadeInDuration);
        }

        if (menuCanvasGroup != null)
            yield return FadeCanvasGroup(menuCanvasGroup, 0f, 1f, menuFadeInDuration);
    }

    IEnumerator FadeInLights()
    {
        foreach (var light in storeLights)
        {
            if (light == null) continue;
            StartCoroutine(FadeLightIntensity(light, 0f, originalIntensities[light], lightsFadeInDuration));
            yield return new WaitForSeconds(delayBetweenLights); // stagger - each light starts a little after the last
        }
        yield return new WaitForSeconds(lightsFadeInDuration); // let the final light finish its own fade before continuing
    }

    // ---- Button handlers - wire these in each Button's OnClick() ----

    public void OnPlayClicked()
    {
        if (isLeaving) return;
        isLeaving = true;
        StopAllCoroutines(); // in case Play is somehow reached while the intro is still running
        StartCoroutine(PlayExitSequence());
    }

    public void OnExitClicked()
    {
        if (isLeaving) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Application.Quit() does nothing in the Editor
#else
        Application.Quit();
#endif
    }

    IEnumerator PlayExitSequence()
    {
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // Fade to black and fade the music out at the same time, so the cut into MapPlacement is smooth.
        blackOverlay.raycastTarget = true;
        if (musicSource != null && musicSource.isPlaying)
            StartCoroutine(FadeAudioVolume(musicSource, musicSource.volume, 0f, exitFadeDuration));
        yield return FadeImage(blackOverlay, blackOverlay.color.a, 1f, exitFadeDuration);

        // Load async so the screen stays black (not frozen) while MapPlacement loads in.
        AsyncOperation op = SceneManager.LoadSceneAsync(mapPlacementSceneName);
        while (!op.isDone) yield return null;
    }

    IEnumerator PullBackCamera()
    {
        Vector3 from = introCamera.position;
        float t = 0f;
        while (t < cameraPullBackDuration)
        {
            t += Time.deltaTime;
            introCamera.position = Vector3.Lerp(from, cameraRestPosition, Mathf.SmoothStep(0f, 1f, t / cameraPullBackDuration));
            yield return null;
        }
        introCamera.position = cameraRestPosition;
    }

    // ---- Small reusable fade helpers ----

    void SetImageAlpha(Image img, float a)
    {
        Color c = img.color;
        img.color = new Color(c.r, c.g, c.b, a);
    }

    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        float t = 0f;
        Color c = img.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetImageAlpha(img, to);
    }

    IEnumerator FadeLightIntensity(Light light, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            light.intensity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        light.intensity = to;
    }

    IEnumerator FadeAudioVolume(AudioSource source, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        source.volume = to;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
        group.interactable = to > 0.5f;   // buttons only become clickable once fully visible
        group.blocksRaycasts = to > 0.5f;
    }
}