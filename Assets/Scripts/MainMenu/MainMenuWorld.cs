using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

namespace GameMenus
{
    public sealed class MainMenuWorld : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [SerializeField] private Transform player;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Transform[] enemies;
        [SerializeField] private Animator[] enemyAnimators;
        [SerializeField] private CinemachineCamera[] cameras;
        [SerializeField] private Light moonLight;
        [SerializeField] private ParticleSystem blood;
        [SerializeField] private AudioSource ambience;
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioClip impact;
        [SerializeField, Min(30f)] private float chaseExtent = 50f;
        [SerializeField] private MainMenuPatrol patrol;
        [SerializeField] private NavMeshObstacle playerObstacle;
        [SerializeField] private AudioClip footstep;
        [SerializeField] private AnimationClip[] introCameraMoves;
        [SerializeField] private Vector3 runPlayback = new(1f, 0.78f, 0.58f);

        private Color targetLight;
        private float targetIntensity;
        private float worldClock;
        private float idleUntil;
        private float nextStep;
        private string selectedAnimation = "Rest";
        private bool menuActive;
        private int shot = -1;
        private Vector3[] catchStarts;
        private bool[] enemyImpacts;
        private float catchElapsed;
        private float impactTime;
        private float catchDirection;
        private int chasePass = -1;

        public bool ImpactStarted { get; private set; }
        public Vector3 ImpactPosition => player.position + Vector3.up;

#if UNITY_EDITOR
        private void Awake()
        {
            foreach (CinemachineCamera camera in cameras)
            {
                camera.gameObject.hideFlags = HideFlags.NotEditable;
            }
        }
#endif


        private void Start()
        {
            targetLight = moonLight.color;
            targetIntensity = moonLight.intensity;
            ambience.loop = true;
            ambience.Play();
            AudioPreferences.Changed += ApplyVolumes;
            ApplyVolumes();
            for (int index = 0; index < enemyAnimators.Length; index++)
            {
                enemyAnimators[index].speed = 0.75f + index * 0.035f;
                enemyAnimators[index].Play("Run", 0, index * 0.137f % 1f);
            }
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            worldClock += delta;
            moonLight.color = Color.Lerp(moonLight.color, targetLight, delta * 2f);
            moonLight.intensity = Mathf.Lerp(moonLight.intensity, targetIntensity, delta * 2f);
            if (!menuActive)
            {
                return;
            }
            if (idleUntil > 0f && worldClock >= idleUntil)
            {
                idleUntil = 0f;
                AnimatePlayer(selectedAnimation);
            }
        }

        public void SetChase(int pass, float progress)
        {
            menuActive = false;
            patrol.StopPatrol();
            playerObstacle.enabled = false;
            SelectCamera(pass);
            introCameraMoves[pass].SampleAnimation(cameras[pass].gameObject, progress * introCameraMoves[pass].length);
            float direction = pass == 1 ? -1f : 1f;
            float extent = Mathf.Max(chaseExtent, chaseExtent * Screen.width / Mathf.Max(1f, Screen.height) / 2.4f);
            float start = -extent * direction;
            float end = pass == 2 ? 3f : extent * direction;
            float travel = pass == 2 ? 1f - Mathf.Pow(1f - progress, 1.45f) : progress;
            float playerX = Mathf.Lerp(start, end, travel);
            float lane = pass == 1 ? 2f : 0f;
            player.position = new Vector3(playerX, 0f, lane);
            player.rotation = Quaternion.Euler(0f, direction > 0f ? 90f : -90f, 0f);
            if (chasePass != pass)
            {
                AnimatePlayer("Run");
            }
            chasePass = pass;
            playerAnimator.speed = runPlayback[pass];
            for (int index = 0; index < enemies.Length; index++)
            {
                float gap = pass == 2 ? Mathf.Lerp(6f, 2f, progress) : pass == 1 ? 5.5f : 7f;
                int row = index / 4;
                enemies[index].position = new Vector3(playerX - direction * (gap + row * 1.7f), 0f, lane + (index % 4 - 1.5f) * 1.05f);
                enemies[index].rotation = player.rotation;
                enemyAnimators[index].SetFloat(SpeedParameter, 5f);
                enemyAnimators[index].speed = Mathf.Max(0.7f, runPlayback[pass] + 0.1f);
            }
            if (worldClock >= nextStep && progress > 0.1f && progress < 0.9f)
            {
                nextStep = worldClock + 0.3f / runPlayback[pass];
                effects.PlayOneShot(footstep, 0.35f);
            }
        }

        public void BeginCatch()
        {
            SelectCamera(3);
            catchStarts = new Vector3[enemies.Length];
            enemyImpacts = new bool[enemies.Length];
            catchElapsed = 0f;
            catchDirection = player.forward.x > 0f ? 1f : -1f;
            ImpactStarted = false;
            playerAnimator.speed = 1f;
            AnimatePlayer("Ready");
            for (int index = 0; index < enemies.Length; index++)
            {
                catchStarts[index] = enemies[index].position;
            }
        }

        public void CatchPlayer(float progress)
        {
            catchElapsed += Time.unscaledDeltaTime;
            for (int index = 0; index < enemies.Length; index++)
            {
                Vector3 start = catchStarts[index];
                float distance = Mathf.Max(0.1f, (player.position.x - start.x) * catchDirection);
                float travel = catchElapsed * (7.5f + index % 3 * 0.4f);
                float convergence = Mathf.Clamp01(travel / distance);
                Vector3 position = new(start.x + catchDirection * travel, 0f, Mathf.Lerp(start.z, player.position.z + (index % 3 - 1) * 0.16f, convergence));
                Vector3 direction = position - enemies[index].position;
                enemies[index].position = position;
                if (direction.sqrMagnitude > 0.00001f)
                {
                    enemies[index].rotation = Quaternion.LookRotation(direction);
                }
            
                enemyAnimators[index].speed = progress >= 1f ? 0f : 1.35f;
                if (!enemyImpacts[index] && (position.x - player.position.x) * catchDirection >= -0.3f)
                {
                    enemyImpacts[index] = true;
                    if (!ImpactStarted)
                    {
                        ImpactStarted = true;
                        impactTime = catchElapsed;
                        AnimatePlayer("Death");
                        Impact();
                    }
                    else
                    {
                        blood.Emit(30);
                    }
                }
            }
            if (ImpactStarted && catchElapsed - impactTime > 0.28f)
            {
                player.gameObject.SetActive(false);
            }
        }

        public void Impact()
        {
            blood.transform.position = player.position + Vector3.up;
            blood.Play(true);
            if (impact != null)
            {
                effects.PlayOneShot(impact, 0.7f);
            }
        }

        public void PrepareMenu(MenuReaction reaction)
        {
            menuActive = true;
            player.gameObject.SetActive(true);
            blood.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            player.position = new Vector3(6f, 0f, 0f);
            player.rotation = Quaternion.Euler(0f, 195f, 0f);
            playerObstacle.enabled = true;
            playerAnimator.speed = 1f;
            React(reaction);
            patrol.StartPatrol();
        }

        public void React(MenuReaction reaction)
        {
            idleUntil = 0f;
            if (reaction == null)
            {
                return;
            }
            selectedAnimation = reaction.animationState;
            AnimatePlayer(selectedAnimation);
            SelectCamera(reaction.cameraIndex);
            targetLight = reaction.lightColor;
            targetIntensity = reaction.lightIntensity;
            patrol.SetActivity(reaction.enemyActivity);
        }


        public void IdleEvent()
        {
            if (!menuActive)
            {
                return;
            }
            AnimatePlayer(Random.value > 0.5f ? "LookAround" : "Prepare");
            idleUntil = worldClock + 3.5f;
        }

        public void SuspendMenu()
        {
            menuActive = false;
            idleUntil = 0f;
            playerObstacle.enabled = false;
        }

        public void ResumeMenu()
        {
            menuActive = true;
            playerObstacle.enabled = true;
            playerAnimator.speed = 1f;
        }

        public void AnimatePlayer(string state)
        {
            int hash = Animator.StringToHash(state);
            if (playerAnimator.HasState(0, hash))
            {
                playerAnimator.CrossFadeInFixedTime(hash, 0.28f);
            }
        }

        public void SelectCamera(int index)
        {
            if (shot == index || index < 0 || index >= cameras.Length)
            {
                return;
            }
            shot = index;
            for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
            {
                cameras[cameraIndex].Priority = cameraIndex == index ? 20 : 0;
            }
        }

        private void ApplyVolumes()
        {
            ambience.volume = 0.38f * AudioPreferences.GetVolume(AudioVolumeChannel.Music);
            effects.volume = AudioPreferences.GetVolume(AudioVolumeChannel.Effects);
        }



        private void OnDestroy()
        {
            AudioPreferences.Changed -= ApplyVolumes;
        }
    }
}
