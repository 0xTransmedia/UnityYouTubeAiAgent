using Inworld;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSpineAnimation : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation _skeletonAnim;
    [SerializeField] private GestureMapSpine _gestureMap;

    private int _selectedIndex = 0;

    public CharacterGestureSpine ActiveGesture { get; private set; }

    private CharacterGestureSpine _idleGesture;
    private InworldCharacter _char;

    private void Awake()
    {
        _char = GetComponent<InworldCharacter>();
        _char.onEmotionChanged.AddListener(EmotionHandle);
        _char.onBeginSpeaking.AddListener(OnBeginSpeaking);
        _char.onEndSpeaking.AddListener(OnEndSpeaking);

        GlobalEvents.SwitchToChatEvent += OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent += OnSwitchToTap;
        GlobalEvents.SetSkinEvent += SetSkin;
        GlobalEvents.FantasyLaunchEvent += OnFantasyLaunch;

        TryGetGesture("NEUTRAL", out var gesture);
        ActiveGesture = gesture;
        _idleGesture = gesture;

        string skinId = PlayerPrefs.GetString(_char.Data.givenName + "Skin");
        if (!string.IsNullOrEmpty(skinId))
            SetSkin(skinId);
    }

    private void OnDestroy()
    {
        _char.onEmotionChanged.RemoveListener(EmotionHandle);
        _char.onBeginSpeaking.RemoveListener(OnBeginSpeaking);
        _char.onEndSpeaking.RemoveListener(OnEndSpeaking);

        GlobalEvents.SwitchToChatEvent -= OnSwitchToChat;
        GlobalEvents.SwitchToTapEvent -= OnSwitchToTap;
        GlobalEvents.SetSkinEvent -= SetSkin;
        GlobalEvents.FantasyLaunchEvent -= OnFantasyLaunch;
    }

    public void EmotionHandle(string strenght, string behavior)
    {
        Debug.Log(behavior);
        if (TryGetGesture(behavior, out var gesture))
        {
            ActiveGesture = gesture;
            UpdateGesture();
        }
        else
            Debug.Log($"Gesture map doesn't contain {behavior} gesture");
    }

    private bool TryGetGesture(string emotion, out CharacterGestureSpine gesture)
    {
        gesture = _gestureMap.Gestures.Find(entry => entry.Name == emotion.ToUpper());
        return gesture != null;
    }

    private void UpdateGesture()
    {
        //Debug.LogError("Update gesture");
        if (_char.IsSpeaking)
            OnBeginSpeaking();
        else OnEndSpeaking();
    }

    private void OnSwitchToChat()
    {
        ResetAnimation();
    }

    private void OnSwitchToTap()
    {
        _char.CancelResponse();
    }

    private void OnBeginSpeaking()
    {
        //Debug.LogError("On speaking begin");
        if (ActiveGesture == null)
            ResetAnimation();
        else _skeletonAnim.AnimationState.SetAnimation(0, ActiveGesture.SpeakAnimation.Animation.Name, true);
    }

    private void OnEndSpeaking()
    {
        //Debug.LogError("On speaking end");
        if (ActiveGesture == null)
            ResetAnimation();
        else _skeletonAnim.AnimationState.SetAnimation(
                0, ActiveGesture.IdleAnimation.Animation.Name, ActiveGesture.Loop)
                .Complete += (value) => ResetAnimation();
    }

    public void ResetAnimation()
    {
        //Debug.LogError("Reset animation");
        _skeletonAnim.AnimationState.SetAnimation(0, _idleGesture.IdleAnimation, true);
    }

    public void SetSkin(string skinId)
    {
        if (_skeletonAnim == null) return;

        _skeletonAnim.Skeleton.SetSkin(skinId);
        _skeletonAnim.Skeleton.SetSlotsToSetupPose();
        _skeletonAnim.AnimationState.Apply(_skeletonAnim.Skeleton);
        
        PlayerPrefs.SetString(_char.Data.givenName + "Skin", skinId);
        PlayerPrefs.Save();
    }

    private void OnFantasyLaunch(FantasySO fantasy)
    {
        if (fantasy == null) return;

        SetSkin(fantasy.Skin.Id);
    }
}
