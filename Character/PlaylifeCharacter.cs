using Inworld.Packet;
using Inworld.Sample.RPM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class PlaylifeCharacter : InworldRPMCharacter
{
    private const string EMOTION = "Emotion";

    [SerializeField] private bool _isPlayer = false;
    [SerializeField] private GestureMap _gestureMap;
    [SerializeField] private Animator _emoteAnimator;

    public bool IsPlayer => _isPlayer;

    public CharacterGesture CurrentGesture { get; private set; }

    private SpriteRenderer _renderer;
    private PolygonCollider2D _collider;
    private List<Vector2> _physicsShape = new List<Vector2>();
    private Vector3 _startRotation;
    protected override void Awake()
    {
        base.Awake();

        _renderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<PolygonCollider2D>();
        _startRotation = transform.rotation.eulerAngles;

        if (_gestureMap == null)
            Debug.LogWarning($"GestureMap is not set for {name}!");
        else
            SetGesture(_gestureMap.Gestures[0]);
    }

    public void Active(bool active)
    {
        if (_emoteAnimator)
        {
            if (!active)
            {
                transform.rotation = Quaternion.Euler(_startRotation);
                _emoteAnimator.SetInteger(EMOTION, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
            }
        }

        if (!_isPlayer)
        {
            //if (active)
            //    _light.enabled = true;
            //else _light.enabled = false;
        }
    }

    protected override void HandleEmotion(EmotionPacket packet)
    {
        base.HandleEmotion(packet);

        string gesture = packet.emotion.behavior;

        if (_gestureMap == null) return;
        if (CurrentGesture != null && CurrentGesture.Name == gesture) return;
        
        CharacterGesture gest = _gestureMap.Gestures.Find(entry => entry.Name == gesture);
        
        ProcessEmotion(gest);
        SetGesture(gest);
    }

    private void SetGesture(CharacterGesture gest)
    {
        Sprite sprite = null;

        if (gest != null)
            sprite = gest.Sprite;

        if (sprite != null)
        {
            StartCoroutine(SpriteTransition(sprite));
            //_renderer.sprite = sprite;
            CurrentGesture = gest;
        }
        else
            Debug.LogWarning($"{name} doesn't contain sprite in {gest.Name} gesture!");
    }

    private void ProcessEmotion(CharacterGesture gesture)
    {
        if (_emoteAnimator)
            _emoteAnimator.SetInteger(EMOTION, (int)gesture.emoteAnimation);
    }

    private IEnumerator SpriteTransition(Sprite newSpite)
    {
        float alpha = 1f;
        while (alpha > 0.1)
        {
            alpha = Mathf.Lerp(alpha, 0, 0.3f);
            _renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return new WaitForSeconds(0.001f);
        }

        alpha = 0;
        _renderer.color = new Color(1f, 1f, 1f, alpha);
        _renderer.sprite = newSpite;
        
        while (alpha < 0.99f)
        {
            alpha = Mathf.Lerp(alpha, 1, 0.3f);
            _renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return new WaitForSeconds(0.001f);
        }

        alpha = 1;
        _renderer.color = new Color(1f, 1f, 1f, alpha);

        _renderer.sprite.GetPhysicsShape(0, _physicsShape);
        _collider.pathCount = 0;
        _collider.SetPath(0, _physicsShape);
    }
}
