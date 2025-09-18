using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Cell : MonoBehaviour
{
    [SerializeField] Image backGround;
    [SerializeField] TextMeshProUGUI valueText;
    [SerializeField] TextMeshProUGUI indexText;

    private int index;
    private int value;

    public int Index
    {
        get => index;
        set
        {
            index = value;
            if (indexText != null)
                indexText.text = index.ToString();
        }
    }

    public int Value
    {
        get => value;
        set
        {
            this.value = value;
            if (valueText != null)
                valueText.text = value.ToString();
        }
    }

    #region Animations
    public void PopIn(float duration = 0.2f)
    {
        StartCoroutine(PopInRoutine(duration));
    }

    private IEnumerator PopInRoutine(float duration)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        float t = 0;
        transform.localScale = startScale;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }
        transform.localScale = endScale;
    }

    public void PopOut(System.Action onFinished, float duration = 0.2f)
    {
        StartCoroutine(PopOutRoutine(onFinished, duration));
    }

    private IEnumerator PopOutRoutine(System.Action onFinished, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }
        transform.localScale = endScale;
        onFinished?.Invoke();
    }

    public void FlashColor(Color color, float duration = 0.5f)
    {
        StartCoroutine(FlashColorRoutine(color, duration));
    }

    private IEnumerator FlashColorRoutine(Color color, float duration)
    {
        if (backGround == null) yield break;
        Color original = backGround.color;
        backGround.color = color;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            backGround.color = Color.Lerp(color, original, t / duration);
            yield return null;
        }
        backGround.color = original;
    }

    // 🔹 Update animation — moves value up, fades out, changes text, drops back
    public void AnimateValueChange(int newValue, float duration = 0.3f)
    {
        StartCoroutine(UpdateValueRoutine(newValue, duration));
    }

    private IEnumerator UpdateValueRoutine(int newValue, float duration)
    {
        Vector3 startPos = valueText.rectTransform.localPosition;
        Vector3 upPos = startPos + Vector3.up * 20f;
        Color startColor = valueText.color;

        // up + fade out
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            valueText.rectTransform.localPosition = Vector3.Lerp(startPos, upPos, n);
            valueText.color = new Color(startColor.r, startColor.g, startColor.b, 1 - n);
            yield return null;
        }

        // set new value at top invisible
        this.value = newValue;
        valueText.text = newValue.ToString();
        valueText.rectTransform.localPosition = upPos;
        valueText.color = new Color(startColor.r, startColor.g, startColor.b, 0);

        // drop down + fade in
        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = t / duration;
            valueText.rectTransform.localPosition = Vector3.Lerp(upPos, startPos, n);
            valueText.color = new Color(startColor.r, startColor.g, startColor.b, n);
            yield return null;
        }

        valueText.rectTransform.localPosition = startPos;
        valueText.color = startColor;
    }
    #endregion
}
