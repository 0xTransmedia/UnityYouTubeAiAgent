using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GestureMapSpine", menuName = "Character/New spine gesture map", order = 2)]

public class GestureMapSpine : ScriptableObject
{
    public List<CharacterGestureSpine> Gestures =
          new List<CharacterGestureSpine>()
          {
            new CharacterGestureSpine() {Name = "AFFECTION" },
            new CharacterGestureSpine() {Name = "ANGER" },
            new CharacterGestureSpine() {Name = "CRITICISM" },
            new CharacterGestureSpine() {Name = "DEFENSIVENESS" },
            new CharacterGestureSpine() {Name = "DISGUST" },
            new CharacterGestureSpine() {Name = "INTEREST" },
            new CharacterGestureSpine() {Name = "JOY" },
            new CharacterGestureSpine() {Name = "NEUTRAL" },
            new CharacterGestureSpine() {Name = "SADNESS" },
            new CharacterGestureSpine() {Name = "SURPRISE" },
            new CharacterGestureSpine() {Name = "VALIDATION" },
            new CharacterGestureSpine() {Name = "WHINING" },
            //new CharacterGestureSpine() {Name = "BELLIGERENCE" },
            //new CharacterGestureSpine() {Name = "HUMOR" },
            //new CharacterGestureSpine() {Name = "STONEWALLING" },
            //new CharacterGestureSpine() {Name = "TENSE" },
            //new CharacterGestureSpine() {Name = "CONTEMPT" },
            //new CharacterGestureSpine() {Name = "DOMINEERING" },
            //new CharacterGestureSpine() {Name = "TENSION" },
          };
}

[Serializable]
public class CharacterGestureSpine
{
    public string Name;
    public AnimationReferenceAsset IdleAnimation;
    public AnimationReferenceAsset SpeakAnimation;
    public bool Loop = false;
}
