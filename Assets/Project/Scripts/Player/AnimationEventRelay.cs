using UnityEngine;

public sealed class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private SwordWeapon swordWeapon;

    public void ApplyHit()
    {
        swordWeapon.ApplyHit();
    }

    public void PlaySwordSwing()
    {
        swordWeapon.PlaySwingSound();
    }
}
