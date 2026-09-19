using UnityEngine;
using TMPro;
using System.Collections;

public class CashPopupItem : MonoBehaviour
{
    public float slideDistance = 40f;
    public float duration = 1f;

    TMP_Text text;
    RectTransform rect;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        rect = GetComponent<RectTransform>();
    }

    public void Show(float delta)
    {
        bool isGain = delta >= 0f;
        text.color = isGain ? Color.green : Color.red;
        text.text = (isGain ? "+" : "-") + $"Rp {Mathf.Abs(delta):N0}";
        StartCoroutine(SlideAndFade());
    }

    IEnumerator SlideAndFade()
    {
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, slideDistance);
        Color startColor = text.color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, progress));
            yield return null;
        }

        Destroy(gameObject);
    }
}