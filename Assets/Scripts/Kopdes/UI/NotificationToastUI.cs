using UnityEngine;
using TMPro;
using System.Collections;

// Update: full rewrite - the old version animated THIS object's own RectTransform, but this
// object is a direct child of the container's Vertical Layout Group, which ALSO controls that
// same RectTransform's position on every layout pass. Two systems fighting over one property is
// exactly what caused a new toast to sit at its raw prefab-authored position until some
// unrelated event (another toast spawning) forced a layout rebuild that "snapped" it into place.
// Fix: animate a NESTED child's position instead - the layout group only ever controls its own
// DIRECT children, so a grandchild's local position is completely invisible to it and safe to
// animate freely, while this object's own position stays entirely the layout group's job.
public class NotificationToastUI : MonoBehaviour
{
    public RectTransform visualRoot; // a CHILD of this object - NOT this object's own RectTransform
    public CanvasGroup canvasGroup;  // lives on visualRoot (or a child of it)
    public TMP_Text messageText;
    public float slideDistance = 20f;
    public float slideInDuration = 0.25f;
    public float holdDuration = 2.5f;
    public float fadeOutDuration = 0.4f;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = visualRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = visualRoot.gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(string message, System.Action onDismissed)
    {
        messageText.text = message;
        StartCoroutine(PlaySequence(onDismissed));
    }

    IEnumerator PlaySequence(System.Action onDismissed)
    {
        // Slide up + fade in - all relative to visualRoot's own LOCAL position within its parent
        // (this object), which the outer layout group never touches at all.
        Vector2 restingPos = visualRoot.anchoredPosition;
        Vector2 startPos = restingPos - new Vector2(0, slideDistance);
        visualRoot.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;

        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.deltaTime;
            float p = t / slideInDuration;
            visualRoot.anchoredPosition = Vector2.Lerp(startPos, restingPos, p);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, p);
            yield return null;
        }
        visualRoot.anchoredPosition = restingPos;
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