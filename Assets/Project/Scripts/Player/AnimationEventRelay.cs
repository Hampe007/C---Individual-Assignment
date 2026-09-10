using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerMagicSystem magicSystem;
    [SerializeField] private SwordWeapon swordWeapon;

    public void ReleaseSpell()
    {
        magicSystem.ReleaseSpell();
    }
    
    public void ApplyHit()
    {
        swordWeapon.ApplyHit();
    }
    
    public void PlaySwordSwing()
    {
        swordWeapon.PlaySwingSound();
    }
}
