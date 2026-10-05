using System;
using UnityEngine;

namespace GameMenus
{
    [CreateAssetMenu(fileName = "MainMenu-Settings", menuName = "Game/Main Menu/Settings")]
    public sealed class MainMenuSettings : ScriptableObject
    {
        public string gameScene = "Game";
        public string tutorialScene = "Tutorial";
        public bool tutorialAvailable;
        [Min(1f)] public float chasePassDuration = 4f;
        [Min(1f)] public float returnPassDuration = 10f;
        [Min(1f)] public float finalPassDuration = 6f;
        [Min(0f)] public float passPause = 0.4f;
        [Min(0.1f)] public float catchDuration = 1.15f;
        [Min(0.1f)] public float bloodCoverDuration = 0.9f;
        [Min(0.1f)] public float bloodDrainDuration = 2.2f;
        [Min(0.1f)] public float skipHoldDuration = 1f;
        public Vector2 idleEventInterval = new(9f, 17f);
        [TextArea(4, 12)] public string credits = "DEVELOPMENT\nAdd your name here\n\nCHARACTERS & ANIMATIONS\nQuaternius\n\nUI & PARTICLE ASSETS\nKenney\n\nAdditional credits can be added here.";
        public MenuReaction[] reactions;

        public MenuReaction GetReaction(MenuOption option)
        {
            if (reactions == null)
            {
                return null;
            }
            foreach (MenuReaction reaction in reactions)
            {
                if (reaction.option == option)
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
        public MenuOption option;
        public string animationState = "Rest";
        public int cameraIndex;
        public int pressedCameraIndex = -1;
        public Color lightColor = new(0.75f, 0.16f, 0.2f);
        [Range(0f, 4f)] public float lightIntensity = 0.34f;
        [Range(0.1f, 2f)] public float enemyActivity = 0.9f;
    }

    public enum MenuOption { None, Play, Tutorial, Options, Credits, Quit }
}