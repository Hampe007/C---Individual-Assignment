using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerMagicSystem magicSystem;

    public void ReleaseSpell()
    {
        magicSystem.ReleaseSpell();
    }
}
