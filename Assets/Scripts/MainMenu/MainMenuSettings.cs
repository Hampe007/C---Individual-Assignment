using System;
using UnityEngine;

namespace GameMenus
{
    [CreateAssetMenu(fileName = "MainMenu-Settings", menuName = "Game/Main Menu/Settings")]
    public sealed class MainMenuSettings : ScriptableObject
    {
        public string GameScene = "Game";
        public string TutorialScene = "Tutorial";
        public bool TutorialAvailable;
        [Min(1f)] public float ChasePassDuration = 4f;
        [Min(1f)] public float ReturnPassDuration = 10f;
        [Min(1f)] public float FinalPassDuration = 6f;
        [Min(0f)] public float PassPause = 0.4f;
        [Min(0.1f)] public float CatchDuration = 1.15f;
        [Min(0.1f)] public float BloodCoverDuration = 0.9f;
        [Min(0.1f)] public float BloodDrainDuration = 2.2f;
        [Min(0.1f)] public float SkipHoldDuration = 1f;
        public Vector2 IdleEventInterval = new(9f, 17f);
        [SerializeField, TextArea(8, 30)] private string creditsFallback;
        public TextAsset CreditsSource;
        public string Credits => CreditsSource != null ? CreditsSource.text : creditsFallback;
        public MenuReaction[] Reactions;

        public MenuReaction GetReaction(MenuOption option)
        {
            if (Reactions == null)
            {
                return null;
            }
            foreach (MenuReaction reaction in Reactions)
            {
                if (reaction.Option == option)
                {
                    return reaction;
                }
            }
            return null;
        }
    }

    [Serializable]
    public sealed class MenuReaction
    {
        public MenuOption Option;
        public string AnimationState = "Rest";
        public int CameraIndex;
        public int PressedCameraIndex = -1;
        public Color LightColor = new(0.75f, 0.16f, 0.2f);
        [Range(0f, 4f)] public float LightIntensity = 0.34f;
        [Range(0.1f, 2f)] public float EnemyActivity = 0.9f;
    }

    public enum MenuOption { None, Play, Tutorial, Options, Credits, Quit }
}
