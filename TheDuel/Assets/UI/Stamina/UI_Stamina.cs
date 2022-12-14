using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stamina : MonoBehaviour
{
    public bool isPlayer;
    public bool startFromLeft;
    public Image fillImage;
    public Image deltaImage;
    public Image backgroundImage;
    public RectTransform boxTransform;
    public RectTransform glowTransform;
    public CanvasGroup glowCanvasGroup;
    public float glowAlphaSpeed = 4f;
    public float glowScaleSpeed = 4f;

    public float fillSmoothTime = 0.1f;
    public float deltaSpeed = 0.5f;

    public float notEnoughStaminaPunchScale;
    public float notEnoughStaminaDuration = 0.1f;
    public Color notEnoughStaminaBackgroundColor;

    private Color _backgroundOriginalColor;
    private float _cv;
    private float _lastStaminaCheckFailTime = float.NegativeInfinity;
    
    private void Start()
    {
        GameManager.instance.onRoundPrepare += () => deltaImage.fillAmount = 1f;
        if (isPlayer) Player.instance.onStaminaCheckFail += HandleStaminaCheckFail;
        else Enemy.instance.onStaminaCheckFail += HandleStaminaCheckFail;
        _backgroundOriginalColor = backgroundImage.color;
    }

    private void Update()
    {
        var normalized = isPlayer ? Player.instance.GetStamina() / Player.MaxStamina : Enemy.instance.GetStamina() / Enemy.MaxStamina;

        var isDecreasing = normalized + 0.001f < fillImage.fillAmount;
        glowCanvasGroup.alpha = Mathf.MoveTowards(glowCanvasGroup.alpha, isDecreasing ? 1 : 0,
            Time.unscaledDeltaTime * glowAlphaSpeed);
        glowTransform.localScale = Mathf.MoveTowards(glowTransform.localScale.x, isDecreasing ? 1 : 0,
            Time.unscaledDeltaTime * glowScaleSpeed) * Vector3.one;
        
        fillImage.fillAmount = Mathf.SmoothDamp(fillImage.fillAmount, normalized, ref _cv, fillSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        deltaImage.fillAmount =
            Mathf.MoveTowards(deltaImage.fillAmount, fillImage.fillAmount, deltaSpeed * Time.unscaledDeltaTime);
        var aPos = glowTransform.anchoredPosition;
        aPos.x = (startFromLeft ? fillImage.fillAmount : 1 - fillImage.fillAmount) * boxTransform.rect.width;
        glowTransform.anchoredPosition = aPos;
    }

    private void HandleStaminaCheckFail()
    {
        if (Time.unscaledTime - _lastStaminaCheckFailTime < notEnoughStaminaDuration) return;
        _lastStaminaCheckFailTime = Time.unscaledTime;
        AnimateNotEnoughStamina();
    }

    private void AnimateNotEnoughStamina()
    {
        transform.DOPunchScale(Vector3.one * notEnoughStaminaPunchScale, notEnoughStaminaDuration).SetUpdate(true);
        backgroundImage.color = notEnoughStaminaBackgroundColor;
        backgroundImage.DOColor(_backgroundOriginalColor, notEnoughStaminaDuration).SetUpdate(true);
    }
}
