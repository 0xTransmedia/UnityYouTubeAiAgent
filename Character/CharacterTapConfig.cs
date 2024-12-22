using Spine.Unity;
using TapGame;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tap config", menuName = "Character/Character Tap config")]
public class CharacterTapConfig : ScriptableObject
{
    [Header("Animations")]
    [SerializeField] private AnimationReferenceAsset _idleAnimaiton;
    [SerializeField] private AnimationReferenceAsset _speakAnimaiton;
    [SerializeField] private AnimationReferenceAsset _slowAnimaiton;
    [SerializeField] private AnimationReferenceAsset _fastAnimaiton;

    [Header("Sounds")]
    [SerializeField] private AudioClip _slowOrgasmClip;
    [SerializeField] private AudioClip _fastOrgasmClip;
    [SerializeField] private AudioClip _stopOrgasmClip;
    [SerializeField] private AudioClip[] _phrasesClips;

    public AnimationReferenceAsset IdleAnimaiton => _idleAnimaiton;
    public AnimationReferenceAsset SpeakAnimaiton=> _speakAnimaiton;
    public AnimationReferenceAsset SlowAnimaiton => _slowAnimaiton; 
    public AnimationReferenceAsset FastAnimaiton => _fastAnimaiton;
    public AudioClip SlowOrgasmClip => _slowOrgasmClip;
    public AudioClip FastOrgasmClip => _fastOrgasmClip;
    public AudioClip StopOrgasmClip => _stopOrgasmClip;
    public AudioClip[] PhrasesClips => _phrasesClips;
}
