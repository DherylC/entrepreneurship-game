using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequenceController : MonoBehaviour
{
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
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }
        if (musicSource != null) musicSource.volume = 0f;

        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(initialBlackHoldDuration);

        yield return FadeImage(blackOverlay, 1f, 0f, blackFadeOutDuration);

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