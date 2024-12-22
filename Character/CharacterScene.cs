using Spine.Unity;
using UnityEngine;

public class CharacterScene : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation _skeleton;
    [SerializeField] private SpriteRenderer _locationRenderer;

    public SkeletonAnimation Skeleton => _skeleton;

    private void OnEnable()
    {
        GlobalEvents.FantasyLaunchEvent += OnFantasyLaunch;
    }

    private void OnDisable()
    {
        GlobalEvents.FantasyLaunchEvent -= OnFantasyLaunch;
    }

    private void OnFantasyLaunch(FantasySO fantasy)
    {
        if (fantasy == null) return;

        _locationRenderer.sprite = fantasy.LocationSprite;
    }
}
