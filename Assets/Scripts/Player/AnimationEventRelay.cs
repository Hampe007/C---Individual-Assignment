using UnityEngine;

public sealed class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private PlayerMovement playerMovement;

    public void ApplyHit()
    {
        swordWeapon.ApplyHit();
    }

    public void PlaySwordSwing()
    {
        swordWeapon.PlaySwingSound();
    }

    public void PlayFootstep()
    {
        if (playerMovement != null)
        {
            playerMovement.PlayFootstep();
        }
    }
}
