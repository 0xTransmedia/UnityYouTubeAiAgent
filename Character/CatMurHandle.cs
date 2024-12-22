using UnityEngine;

public class CatMurHandle : MonoBehaviour
{
    private const string TRIGGER_MUR = "MUR", TRIGGER_IDLE = "NEUTRAL"; 
    
    [SerializeField] private Camera _camera;
    [SerializeField] private AudioClip _murSound;
    [SerializeField] private ParticleSystem _particles;
    
    private CharacterSpineAnimation _character;
    private AudioSource _audio;

    private bool _isMur = false;
    private Touch _touch;


    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _character = GetComponent<CharacterSpineAnimation>();
    }

    private void Update()
    {
        if (Input.touches.Length == 0) return;
        
        _touch = Input.touches[0];
        if (_touch.phase == TouchPhase.Moved)
            SwipeHandle();
        else if (_touch.phase == TouchPhase.Ended
            || _touch.phase == TouchPhase.Canceled)
            Stop();
    }

    private void SwipeHandle()
    {
        Ray ray = _camera.ScreenPointToRay(_touch.position);
        Physics.Raycast(ray, out RaycastHit hitInfo, 50f);
        if (_character == hitInfo.collider?.GetComponent<CharacterSpineAnimation>())
        {
            _particles?.Play();
            var touchInWorldPos = _camera.ScreenToWorldPoint(_touch.position);
            var particlePosition = new Vector3(touchInWorldPos.x, touchInWorldPos.y, -1);
            _particles.transform.position = particlePosition;
            Mur(_character);
        }
        else Stop();
    }

    private void Mur(CharacterSpineAnimation character)
    {
        if (_isMur) return;

        _isMur = true;
        character.EmotionHandle("null", TRIGGER_MUR);
        _audio.PlayOneShot(_murSound); 
    }

    private void Stop()
    {
        if (_character == null || !_isMur) return;

        _isMur = false;
        _audio.Stop();
        _particles?.Stop();
        _character.EmotionHandle("null", TRIGGER_IDLE);
    }
}
