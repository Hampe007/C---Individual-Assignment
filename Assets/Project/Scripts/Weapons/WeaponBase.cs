using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float _cooldown = 1f;

    private float _cooldownTimer;

    protected virtual void Update()
    {
        if (Time.timeScale == 0f)
            return;
        
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if (TryAttack())
            _cooldownTimer = _cooldown;
    }

    protected abstract bool TryAttack();
    
    public void ReduceCooldown(float amount)
    {
        if (amount <= 0f)
            return;

        _cooldown = Mathf.Max(0.05f, _cooldown - amount);
    }
}