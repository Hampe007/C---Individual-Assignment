using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float _cooldown = 1f;

    private float _cooldownTimer;

    protected virtual void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if (TryAttack())
            _cooldownTimer = _cooldown;
    }

    protected abstract bool TryAttack();
}