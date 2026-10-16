using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyHitFlash : MonoBehaviour
{
    private struct MaterialSlot
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public int ColorProperty;
        public Color OriginalColor;
        public MaterialPropertyBlock Block;
        public MaterialSlot(Renderer renderer, int materialIndex, int colorProperty, Color originalColor)
        {
            Renderer = renderer;
            MaterialIndex = materialIndex;
            this.ColorProperty = colorProperty;
            OriginalColor = originalColor;
            Block = new MaterialPropertyBlock();
        }
    }

    [SerializeField, Min(0.01f)] private float duration = 0.1f;
    [SerializeField, Range(0f, 1f)] private float strength = 0.7f;
    private readonly List<MaterialSlot> slots = new();
    private float timer;
    private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int colorProperty = Shader.PropertyToID("_Color");
    private void Awake()
    {
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                int property;
                if (material.HasProperty(baseColor))
                {
                    property = baseColor;
                }
                else if (material.HasProperty(colorProperty))
                {
                    property = colorProperty;
                }
                else
                {
                    continue;
                }

                slots.Add(new MaterialSlot(renderer, i, property, material.GetColor(property)));
            }
        }

        enabled = false;
    }

    internal void Play()
    {
        timer = duration;
        foreach (MaterialSlot slot in slots)
        {
            slot.Renderer.GetPropertyBlock(slot.Block, slot.MaterialIndex);
            Color flashColor = Color.Lerp(slot.OriginalColor, Color.red, strength);
            slot.Block.SetColor(slot.ColorProperty, flashColor);
            slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
        }

        enabled = true;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        foreach (MaterialSlot slot in slots)
        {
            slot.Renderer.GetPropertyBlock(slot.Block, slot.MaterialIndex);
            slot.Block.SetColor(slot.ColorProperty, slot.OriginalColor);
            slot.Renderer.SetPropertyBlock(slot.Block, slot.MaterialIndex);
        }
    }
}