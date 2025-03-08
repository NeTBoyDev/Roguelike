using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using NaughtyAttributes;
using _Project.Develop.Core.Player;
using TMPro;
using System.Collections;

public enum UIHoverEffectType
{
    Scale,
    Rotation,
    Color,
    Move,
}

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Settings")]
    [SerializeField] private UIHoverEffectType effectType = UIHoverEffectType.Scale;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Scale Settings")]
    [ShowIf(nameof(effectType), UIHoverEffectType.Scale)]
    [SerializeField] private Vector3 hoverScale = new(1.1f, 1.1f, 1.1f);
    [ShowIf(nameof(effectType), UIHoverEffectType.Scale)]
    [SerializeField] private Vector3 normalScale = new(1f, 1f, 1f);

    [Header("Rotation Settings")]
    [ShowIf(nameof(effectType), UIHoverEffectType.Rotation)]
    [SerializeField] private Vector3 hoverRotation = new(0, 0, 5);
    [ShowIf(nameof(effectType), UIHoverEffectType.Rotation)]
    [SerializeField] private Vector3 randomRotationRange = new(5f, 5f, 5f);

    [Header("Color Settings")]
    [ShowIf(nameof(effectType), UIHoverEffectType.Color)]
    [SerializeField] private Color hoverColor = Color.white;
    [ShowIf(nameof(effectType), UIHoverEffectType.Color)]
    [SerializeField] private Vector3 randomColorRange = new(0.2f, 0.2f, 0.2f);
    [ShowIf(nameof(effectType), UIHoverEffectType.Color)]
    [SerializeField] private bool affectImage = true;
    [ShowIf(nameof(effectType), UIHoverEffectType.Color)]
    [SerializeField] private bool affectText = true;
    [ShowIf(nameof(effectType), UIHoverEffectType.Color)]
    [SerializeField] private bool affectChildText = true;

    [Header("Move Settings")]
    [ShowIf(nameof(effectType), UIHoverEffectType.Move)]
    [SerializeField] private Vector3 hoverOffset = new(0, 5, 0);

    [SerializeField] private AudioClip EffectSound = null;

    private UnityEngine.UI.Image image;
    private TMPro.TMP_Text text;
    private TMPro.TMP_Text childText;

    private Vector3 startPosition;
    private Color startColor;
    private Color childTextStartColor;
    private Tweener currentTween;
    private SoundManager soundManager;


    private void Awake()
    {
        image = GetComponent<UnityEngine.UI.Image>();
        text = GetComponent<TMPro.TMP_Text>();
        childText = GetComponentInChildren<TMPro.TMP_Text>();
        soundManager = new SoundManager(0.5f);

        startPosition = transform.localPosition;
        if (image != null)
            startColor = image.color;
        else if (text != null)
            startColor = text.color;

        if (childText != null)
            childTextStartColor = childText.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentTween?.Kill();

        switch (effectType)
        {
            case UIHoverEffectType.Scale:
                currentTween = transform.DOScale(hoverScale, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Rotation:
                Vector3 randomRotation = new Vector3(
                    Random.Range(-randomRotationRange.x, randomRotationRange.x),
                    Random.Range(-randomRotationRange.y, randomRotationRange.y),
                    Random.Range(-randomRotationRange.z, randomRotationRange.z)
                );
                Vector3 finalRotation = hoverRotation + randomRotation;
                currentTween = transform.DOLocalRotate(finalRotation, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Color:
                Color finalColor = hoverColor;
                finalColor = new Color(
                    hoverColor.r + Random.Range(-randomColorRange.x, randomColorRange.x),
                    hoverColor.g + Random.Range(-randomColorRange.y, randomColorRange.y),
                    hoverColor.b + Random.Range(-randomColorRange.z, randomColorRange.z),
                    hoverColor.a
                );

                finalColor = new Color(
                    Mathf.Clamp01(finalColor.r),
                    Mathf.Clamp01(finalColor.g),
                    Mathf.Clamp01(finalColor.b),
                    Mathf.Clamp01(finalColor.a)
                );

                if (affectImage && image != null)
                    currentTween = image.DOColor(finalColor, duration).SetEase(easeType);
                if (affectText && text != null)
                    currentTween = text.DOColor(finalColor, duration).SetEase(easeType);
                if (affectChildText && childText != null)
                    childText.DOColor(finalColor, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Move:
                currentTween = transform.DOLocalMove(startPosition + hoverOffset, duration).SetEase(easeType);
                break;
        }

        soundManager.ProduceSound(transform.position, EffectSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();

        switch (effectType)
        {
            case UIHoverEffectType.Scale:
                currentTween = transform.DOScale(normalScale, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Rotation:
                currentTween = transform.DOLocalRotate(Vector3.zero, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Color:
                if (affectImage && image != null)
                    currentTween = image.DOColor(startColor, duration).SetEase(easeType);
                if (affectText && text != null)
                    currentTween = text.DOColor(startColor, duration).SetEase(easeType);
                if (affectChildText && childText != null)
                    childText.DOColor(childTextStartColor, duration).SetEase(easeType);
                break;

            case UIHoverEffectType.Move:
                currentTween = transform.DOLocalMove(startPosition, duration).SetEase(easeType);
                break;
        }
    }
}