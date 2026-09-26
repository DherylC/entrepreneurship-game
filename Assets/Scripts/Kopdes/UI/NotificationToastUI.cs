using UnityEngine;
using TMPro;
using System.Collections;

public class NotificationToastUI : MonoBehaviour
{
    public TMP_Text messageText;
    public float slideDistance = 20f;
    public float slideInDuration = 0.25f;
    public float holdDuration = 2.5f;
    public float fadeOutDuration = 0.4f;

    RectTransform rect;
    CanvasGroup canvasGroup;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(string message, System.Action onDismissed)
    {
        messageText.text = message;
        StartCoroutine(PlaySequence(onDismissed));
    }

    IEnumerator PlaySequence(System.Action onDismissed)
    {
        // Slide up + fade in
        Vector2 startPos = rect.anchoredPosition - new Vector2(0, slideDistance);
        Vector2 endPos = rect.anchoredPosition;
        rect.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.deltaTime;
            float p = t / slideInDuration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }
        rect.anchoredPosition = endPos;
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        // Fade out in place - the container's Vertical Layout Group re-flows the remaining
        // stack automatically the instant this object is destroyed below.
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }

        onDismissed?.Invoke();
        Destroy(gameObject);
    }
}