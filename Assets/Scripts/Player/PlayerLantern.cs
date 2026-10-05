using UnityEngine;

public sealed class PlayerLantern : MonoBehaviour
{
    [SerializeField] private GameObject _lanternPrefab;
    [SerializeField] private Vector3 _offset = new(1.3f, 1.5f, 0.5f);
    [SerializeField, Min(0f)] private float _minimumPlayerDistance = 1.15f;
    [SerializeField, Min(0f)] private float _followSmoothTime = 0.12f;
    [SerializeField, Min(0f)] private float _initialRange = 5f;
    [SerializeField, Min(0f)] private float _maximumRange = 9.5f;
    [SerializeField, Min(0f)] private float _bobAmplitude = 0.08f;
    [SerializeField, Min(0.1f)] private float _bobPeriod = 3f;
    private GameObject _lantern;
    private Light _light;
    private Vector3 _followPosition;
    private Vector3 _velocity;
    private Vector3 _previousPlayerPosition;
    private float _elapsed;

    public float Range => _light != null ? _light.range : 0f;
    public bool IsUnlocked => _light != null;
    public bool CanUnlock => isActiveAndEnabled && !IsUnlocked &&
        _lanternPrefab != null && _lanternPrefab.GetComponentInChildren<Light>(true) != null;
    public bool IsReady => isActiveAndEnabled && _light != null;

    private void Awake()
    {
        if (_lanternPrefab == null || _lanternPrefab.GetComponentInChildren<Light>(true) == null)
        {
            Debug.LogError("PlayerLantern requires a lantern prefab with a light.", this);
            enabled = false;
        }
    }

    public bool TryUnlock()
    {
        if (!CanUnlock)
            return false;
        _followPosition = KeepOutsidePlayer(transform.position + _offset);
        _previousPlayerPosition = transform.position;
        _velocity = Vector3.zero;
        _elapsed = 0f;
        _lantern = Instantiate(_lanternPrefab, _followPosition, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_lantern, gameObject.scene);
        _light = _lantern.GetComponentInChildren<Light>(true);
        if (_light == null)
        {
            Destroy(_lantern);
            _lantern = null;
            return false;
        }
        _lantern.SetActive(true);
        _light.enabled = true;
        _light.range = _initialRange;
        return true;
    }

    private void OnEnable()
    {
        if (_lantern == null)
            return;
        _lantern.SetActive(true);
        _followPosition = KeepOutsidePlayer(transform.position + _offset);
        _previousPlayerPosition = transform.position;
        _velocity = Vector3.zero;
        _lantern.transform.position = _followPosition;
    }

    private void LateUpdate()
    {
        if (_lantern == null || Time.timeScale <= 0f || Time.deltaTime <= 0f)
            return;
        UpdateFollow(Time.deltaTime);
    }

    private void UpdateFollow(float deltaTime)
    {
        Vector3 target = KeepOutsidePlayer(transform.position + _offset);
        Vector3 displacement = transform.position - _previousPlayerPosition;
        if (displacement.sqrMagnitude > 9f)
        {
            _followPosition = target;
            _velocity = Vector3.zero;
        }
        else
        {
            // Carry the lantern with the player instead of letting movement pull it through the body.
            _followPosition += displacement;
            _followPosition = Vector3.SmoothDamp(_followPosition, target, ref _velocity, _followSmoothTime, Mathf.Infinity, deltaTime);
        }
        _followPosition = KeepOutsidePlayer(_followPosition);
        _previousPlayerPosition = transform.position;
        _elapsed += deltaTime;
        float bob = Mathf.Sin(_elapsed * Mathf.PI * 2f / _bobPeriod) * _bobAmplitude;
        _lantern.transform.position = _followPosition + Vector3.up * bob;
    }

    private Vector3 KeepOutsidePlayer(Vector3 position)
    {
        Vector3 horizontal = position - transform.position;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude >= _minimumPlayerDistance * _minimumPlayerDistance)
            return position;

        Vector3 direction = horizontal.sqrMagnitude > 0.0001f ? horizontal : new Vector3(_offset.x, 0f, _offset.z);
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.right;
        return position + direction.normalized * _minimumPlayerDistance - horizontal;
    }

    public void IncreaseRange(float amount)
    {
        if (_light != null)
            _light.range = Mathf.Clamp(_light.range + Mathf.Max(0f, amount), _initialRange, _maximumRange);
    }

    private void OnDisable()
    {
        if (_lantern != null)
            _lantern.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_lantern != null)
            Destroy(_lantern);
    }
}
