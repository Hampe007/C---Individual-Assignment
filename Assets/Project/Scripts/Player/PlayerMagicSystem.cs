using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMagicSystem : MonoBehaviour
{
    [Header("Spell")]
    [SerializeField] private Spell spellToCast;
    [SerializeField] private Transform castPoint;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Mana")]
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float currentMana;
    [SerializeField] private float manaRechargeRate = 2f;
    [SerializeField] private float timeToWaitForRecharge = 1f;

    [Header("Casting")]
    [SerializeField] private float timeBetweenCasts = 0.25f;

    private float currentCastTimer;
    private float currentManaRechargeTimer;

    private bool castingMagic;

    private PlayerInput playerInput;
    private InputAction spellCastAction;

    private static readonly int CastSpellHash =
        Animator.StringToHash("CastSpell");

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        spellCastAction = playerInput.actions.FindAction(
            "Spell Cast",
            throwIfNotFound: true
        );

        currentMana = maxMana;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;
        
        bool isSpellCastHeldDown = spellCastAction.IsPressed();

        bool hasEnoughMana =
            currentMana >= spellToCast.spellToCast.manaCost;

        if (!castingMagic && isSpellCastHeldDown && hasEnoughMana)
        {
            castingMagic = true;

            currentMana -= spellToCast.spellToCast.manaCost;

            currentCastTimer = 0f;
            currentManaRechargeTimer = 0f;

            // Start the casting animation.
            animator.SetTrigger(CastSpellHash);

            Debug.Log("Casting spell");
        }

        if (castingMagic && !isSpellCastHeldDown)
        {
            currentCastTimer += Time.deltaTime;

            if (currentCastTimer > timeBetweenCasts)
            {
                castingMagic = false;
            }
        }

        if (currentMana < maxMana &&
            !castingMagic &&
            !isSpellCastHeldDown)
        {
            currentManaRechargeTimer += Time.deltaTime;

            if (currentManaRechargeTimer > timeToWaitForRecharge)
            {
                currentMana += manaRechargeRate * Time.deltaTime;
                currentMana = Mathf.Min(currentMana, maxMana);
            }
        }
    }

    // Called by an Animation Event in SpellCasting_Shoot.
    public void ReleaseSpell()
    {
        Instantiate(
            spellToCast,
            castPoint.position,
            castPoint.rotation
        );
    }
}