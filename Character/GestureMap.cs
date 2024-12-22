using System;
using System.Collections.Generic;
using UnityEngine;
using Inworld.Assets;

[CreateAssetMenu(fileName = "GestureMap", menuName = "Character/New gesture map", order = 1)]
public class GestureMap : ScriptableObject
{
    public List<CharacterGesture> Gestures;
}


[Serializable]
public class CharacterGesture
{
    public string Name;
    public Sprite Sprite;
    public FacialEmotion emoteAnimation;
}