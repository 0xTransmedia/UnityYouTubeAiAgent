using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingForAnswer : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _text;

    private List<Tween> _tweens = new();

    private void OnEnable()
    {
        _icon.rectTransform.localRotation = Quaternion.identity;
        _tweens.Add(_icon.rectTransform.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart));

        _text.color = Color.white;
        _tweens.Add(_text.DOFade(0.5f, 1)
            .SetLoops(-1, LoopType.Yoyo));
    }

    private void OnDisable()
    {
        if (_tweens.Count > 0)
        {
            foreach (var tween in _tweens)
                tween.Kill();
            _tweens.Clear();
        }
    }
}
