using UnityEngine;

public sealed class PlayerOutfitView : MonoBehaviour
{
    [SerializeField] private SkillTreeProgression skillTree;
    [SerializeField] private GameObject[] outfits;

    private void OnEnable()
    {
        if (skillTree != null)
        {
            skillTree.UpgradePurchased += UpdateOutfit;
        }
    }

    private void Start()
    {
        UpdateOutfit();
    }

    private void OnDisable()
    {
        if (skillTree != null)
        {
            skillTree.UpgradePurchased -= UpdateOutfit;
        }
    }

    private void UpdateOutfit()
    {
        if (skillTree == null || outfits == null || outfits.Length == 0)
        {
            return;
        }

        int index = Mathf.Clamp(skillTree.GetLevel("max_health"), 0, outfits.Length - 1);

        for (int i = 0; i < outfits.Length; i++)
        {
            if (outfits[i] != null)
            {
                outfits[i].SetActive(i == index);
            }
        }
    }
}
