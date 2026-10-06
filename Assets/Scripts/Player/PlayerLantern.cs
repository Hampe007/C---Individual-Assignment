using UnityEngine;
using UnityEngine.Serialization;

public sealed class PlayerLantern : MonoBehaviour
{
    [FormerlySerializedAs("_lanternPrefab"), SerializeField] private GameObject lanternPrefab;
    [FormerlySerializedAs("_offset"), SerializeField] private Vector3 offset = new(1.3f, 1.5f, 0.5f);
    [FormerlySerializedAs("_minimumPlayerDistance"), SerializeField, Min(0f)] private float minimumPlayerDistance = 1.15f;
    [FormerlySerializedAs("_followSmoothTime"), SerializeField, Min(0f)] private float followSmoothTime = 0.35f;
    [FormerlySerializedAs("_initialRange"), SerializeField, Min(0f)] private float initialRange = 5f;
    [FormerlySerializedAs("_maximumRange"), SerializeField, Min(0f)] private float maximumRange = 9.5f;
    [FormerlySerializedAs("_bobAmplitude"), SerializeField, Min(0f)] private float bobAmplitude = 0.16f;
    [FormerlySerializedAs("_bobPeriod"), SerializeField, Min(0.1f)] private float bobPeriod = 3f;
    [SerializeField, Min(0f)] private float initialIntensity = 13.5f;
    [SerializeField, Range(0f, 15f)] private float idleOrbitAngle = 6f;
    [SerializeField, Min(0.1f)] private float idleOrbitPeriod = 5f;
    [SerializeField, Min(0f)] private float orbitSpeed = 28f;
    [SerializeField, Range(0f, 0.9f)] private float orbitSpeedVariation = 0.35f;
    [SerializeField, Min(0.1f)] private float orbitSpeedPeriod = 7f;
    [SerializeField, Range(0f, 1f)] private float facingInfluence = 0.35f;
    [SerializeField, Min(0f)] private float radiusDrift = 0.12f;
    [SerializeField, Min(0.1f)] private float radiusDriftPeriod = 4.7f;
    private GameObject lantern;
    private Light lanternLight;
    private Vector3 followPosition;
    private float angularVelocity;
    private float orbitAngle;
    private float desiredOrbitAngle;
    private float previousFacingAngle;
    private Vector3 previousPlayerPosition;
    private float elapsed;

    public float Range => lanternLight != null ? lanternLight.range : 0f;
    public bool IsUnlocked => lanternLight != null;
    public bool CanUnlock => isActiveAndEnabled && !IsUnlocked &&
        lanternPrefab != null && lanternPrefab.GetComponentInChildren<Light>(true) != null;
    public bool IsReady => isActiveAndEnabled && lanternLight != null;

    private void Awake()
    {
        if (lanternPrefab == null || lanternPrefab.GetComponentInChildren<Light>(true) == null)
        {
            Debug.LogError("PlayerLantern requires a lantern prefab with a light.", this);
            enabled = false;
        }
    }

    public bool TryUnlock()
    {
        if (!CanUnlock)
            return false;
        orbitAngle = FacingOrbitAngle();
        desiredOrbitAngle = orbitAngle;
        previousFacingAngle = transform.eulerAngles.y;
        elapsed = 0f;
        followPosition = OrbitPosition(orbitAngle);
        previousPlayerPosition = transform.position;
        angularVelocity = 0f;
        lantern = Instantiate(lanternPrefab, followPosition, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lantern, gameObject.scene);
        lanternLight = lantern.GetComponentInChildren<Light>(true);
        if (lanternLight == null)
        {
            Destroy(lantern);
            lantern = null;
            return false;
        }
        lantern.SetActive(true);
        lanternLight.enabled = true;
        lanternLight.range = initialRange;
        ApplyLightStrength();
        return true;
    }

    private void OnEnable()
    {
        if (lantern == null)
            return;
        lantern.SetActive(true);
        orbitAngle = FacingOrbitAngle();
        desiredOrbitAngle = orbitAngle;
        previousFacingAngle = transform.eulerAngles.y;
        followPosition = OrbitPosition(orbitAngle);
        previousPlayerPosition = transform.position;
        angularVelocity = 0f;
        lantern.transform.position = followPosition;
    }

    private void LateUpdate()
    {
        if (lantern == null || Time.timeScale <= 0f || Time.deltaTime <= 0f)
            return;
        UpdateFollow(Time.deltaTime);
    }

    private void UpdateFollow(float deltaTime)
    {
        Vector3 displacement = transform.position - previousPlayerPosition;
        float facingAngle = transform.eulerAngles.y;
        elapsed += deltaTime;
        if (displacement.sqrMagnitude > 9f)
        {
            orbitAngle = FacingOrbitAngle();
            desiredOrbitAngle = orbitAngle;
            angularVelocity = 0f;
        }
        else
        {
            // Drift continuously around the halo; turning nudges it without fixing it to one side.
            float speed = orbitSpeed * (1f + Mathf.Sin(elapsed * Mathf.PI * 2f / orbitSpeedPeriod) * orbitSpeedVariation);
            float turn = Mathf.DeltaAngle(previousFacingAngle, facingAngle) * facingInfluence;
            desiredOrbitAngle = Mathf.Repeat(desiredOrbitAngle + speed * deltaTime + turn, 360f);
            // Interpolate along the ring instead of cutting a chord through the player's body.
            orbitAngle = Mathf.SmoothDampAngle(orbitAngle, desiredOrbitAngle, ref angularVelocity, followSmoothTime, Mathf.Infinity, deltaTime);
        }
        previousFacingAngle = facingAngle;
        previousPlayerPosition = transform.position;
        float sway = Mathf.Sin(elapsed * Mathf.PI * 2f / idleOrbitPeriod) * idleOrbitAngle;
        followPosition = OrbitPosition(orbitAngle + sway);
        float bob = Mathf.Sin(elapsed * Mathf.PI * 2f / bobPeriod) * bobAmplitude;
        lantern.transform.position = followPosition + Vector3.up * bob;
    }

    private float FacingOrbitAngle() => transform.eulerAngles.y + Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

    private Vector3 OrbitPosition(float angle)
    {
        float drift = Mathf.Sin(elapsed * Mathf.PI * 2f / radiusDriftPeriod) * radiusDrift;
        float radius = Mathf.Max(new Vector2(offset.x, offset.z).magnitude + drift, minimumPlayerDistance);
        float radians = angle * Mathf.Deg2Rad;
        return KeepOutsidePlayer(transform.position + new Vector3(Mathf.Sin(radians) * radius, offset.y, Mathf.Cos(radians) * radius));
    }

    private void ApplyLightStrength()
    {
        // Compensate inverse-square falloff so increasing range also expands useful visibility.
        float scale = lanternLight.range / Mathf.Max(initialRange, 0.01f);
        lanternLight.intensity = initialIntensity * scale * scale;
    }

    private Vector3 KeepOutsidePlayer(Vector3 position)
    {
        Vector3 horizontal = position - transform.position;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude >= minimumPlayerDistance * minimumPlayerDistance)
            return position;

        Vector3 direction = horizontal.sqrMagnitude > 0.0001f ? horizontal : new Vector3(offset.x, 0f, offset.z);
        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.right;
        return position + direction.normalized * minimumPlayerDistance - horizontal;
    }

    public void IncreaseRange(float amount)
    {
        if (lanternLight != null)
        {
            lanternLight.range = Mathf.Clamp(lanternLight.range + Mathf.Max(0f, amount), initialRange, maximumRange);
            ApplyLightStrength();
        }
    }

    private void OnDisable()
    {
        if (lantern != null)
            lantern.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lantern != null)
            Destroy(lantern);
    }
}
