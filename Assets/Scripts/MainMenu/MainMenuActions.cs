using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

namespace GameMenus
{
    public sealed class MainMenuActions : MonoBehaviour
    {
        [SerializeField] private MainMenuWorld world;
        [SerializeField] private NavMeshAgent player;
        [SerializeField] private Transform home;
        [SerializeField] private Transform gateExit;
        [SerializeField] private Transform playFogRunEnd;
        [SerializeField] private Transform playExit;
        [SerializeField] private Transform quitExit;
        [SerializeField] private Transform[] gateRoute;
        [SerializeField] private bool playToGate = true;
        [SerializeField] private CinemachineFollow follow;
        [SerializeField] private CinemachineRotationComposer aim;
        [SerializeField] private CanvasGroup quitFade;
        [SerializeField, Min(0.1f), InspectorName("Menu Run Speed")] private float walkSpeed = 6f;
        [SerializeField, Min(0.1f)] private float quitRunSpeed = 9f;
        [SerializeField, Min(1f)] private float departureDuration = 6.5f;
        [SerializeField, Min(0.1f)] private float happyDuration = 2.5f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;
        [SerializeField] private Vector3 gateFollowOffset = new(4f, 3f, -7f);
        [SerializeField] private Vector3 gateDialogOffset = new(0f, 2.7f, 6f);
        [SerializeField] private Vector3 playFollowOffset = new(6f, 6f, 18f);
        [SerializeField] private Vector3 forwardDialogOffset = new(0f, 2.7f, -6f);
        private bool running;

        public bool ReachedDestination { get; private set; }

        private IEnumerator Begin()
        {
            world.SuspendMenu();
            running = false;
            yield return null;
            yield return null;
            player.enabled = true;
            ReachedDestination = player.isOnNavMesh;
            if (!ReachedDestination)
            {
                Debug.LogWarning("Menu player is outside the baked NavMesh.", this);
            }
            world.SelectCamera(10);
        }

        public IEnumerator ApproachGate()
        {
            Vector3 initialOffset = Camera.main.transform.position - player.transform.position;
            follow.FollowOffset = playToGate ? initialOffset : gateFollowOffset;
            aim.Composition.ScreenPosition = Vector2.zero;
            yield return Begin();
            if (ReachedDestination)
            {
                if (playToGate)
                {
                    yield return WalkTo(playExit.position, true, initialOffset);
                }
                else
                {
                    yield return WalkGate(false);
                }
            }
            world.AnimatePlayer("Rest");
            running = false;
            Vector3 from = follow.FollowOffset;
            Vector3 dialog = playToGate ? forwardDialogOffset : gateDialogOffset;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed);
                follow.FollowOffset = Vector3.Slerp(from, dialog, progress);
                aim.Composition.ScreenPosition = Vector2.Lerp(Vector2.zero, new Vector2(0.23f, -0.05f), progress);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            follow.FollowOffset = dialog;
            aim.Composition.ScreenPosition = new Vector2(0.23f, -0.05f);
        }

        public IEnumerator ReturnHome()
        {
            running = false;
            world.AnimatePlayer("Happy");
            yield return new WaitForSecondsRealtime(happyDuration);
            aim.Composition.ScreenPosition = Vector2.zero;
            if (!playToGate)
            {
                yield return WalkGate(true);
            }
            yield return WalkTo(home.position);
            if (player.enabled)
            {
                player.enabled = false;
            }
            player.transform.SetPositionAndRotation(home.position, home.rotation);
            world.ResumeMenu();
        }

        public IEnumerator LeaveGate()
        {
            aim.Composition.ScreenPosition = Vector2.zero;
            yield return WalkTo(playToGate ? quitExit.position : gateExit.position, runSpeed: quitRunSpeed);
            if (!ReachedDestination)
            {
                yield break;
            }
            quitFade.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                quitFade.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            quitFade.alpha = 1f;
            AudioPreferences.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public IEnumerator Depart()
        {
            aim.Composition.ScreenPosition = Vector2.zero;
            Vector3 initialOffset = Camera.main.transform.position - player.transform.position;
            follow.FollowOffset = playToGate ? gateFollowOffset : initialOffset;
            yield return Begin();
            if (!ReachedDestination)
            {
                yield break;
            }
            if (playToGate)
            {
                yield return WalkGate(false, true);
                if (ReachedDestination)
                {
                    yield return WalkTo(gateExit.position, false);
                    if (ReachedDestination)
                    {
                        var continuation = new NavMeshPath();
                        if (playFogRunEnd != null && player.CalculatePath(playFogRunEnd.position, continuation) && continuation.status == NavMeshPathStatus.PathComplete)
                        {
                            player.SetPath(continuation);
                        }
                        else
                        {
                            player.ResetPath();
                            player.isStopped = true;
                            world.AnimatePlayer("Rest");
                        }
                    }
                }
            
                yield break;
            }
            player.speed = walkSpeed;
            player.isStopped = false;
            player.SetDestination(playExit.position);
            yield return null;
            while (player.pathPending)
            {
                yield return null;
            }
            ReachedDestination = player.pathStatus == NavMeshPathStatus.PathComplete;
            if (!ReachedDestination)
            {
                yield break;
            }
            world.AnimatePlayer("Run");
            float elapsed = 0f;
            while (elapsed < departureDuration)
            {
                follow.FollowOffset = Vector3.Slerp(initialOffset, playFollowOffset, Mathf.SmoothStep(0f, 1f, elapsed / 2f));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator WalkGate(bool reverse, bool continuous = false)
        {
            for (int count = 0; count < gateRoute.Length; count++)
            {
                int index = reverse ? gateRoute.Length - count - 1 : count;
                yield return WalkTo(gateRoute[index].position, count == gateRoute.Length - 1 && !continuous);
                if (!ReachedDestination)
                {
                    yield break;
                }
            }
        }

        private IEnumerator WalkTo(Vector3 destination, bool stopAtEnd = true, Vector3 orbitFrom = default, float runSpeed = 0f)
        {
            ReachedDestination = false;
            if (!player.enabled || !player.isOnNavMesh)
            {
                yield break;
            }
            player.speed = runSpeed > 0f ? runSpeed : walkSpeed;
            player.autoBraking = stopAtEnd;
            player.isStopped = false;
            player.SetDestination(destination);
            if (!running)
            {
                world.AnimatePlayer("Run");
                running = true;
            }
            yield return null;
            float elapsed = 0f;
            while (elapsed < 40f)
            {
                if (orbitFrom != Vector3.zero)
                {
                    follow.FollowOffset = Vector3.Slerp(orbitFrom, playFollowOffset, Mathf.SmoothStep(0f, 1f, elapsed / 2f));
                }
            
                if (!player.pathPending)
                {
                    if (player.pathStatus != NavMeshPathStatus.PathComplete)
                    {
                        break;
                    }
            
                    if (player.remainingDistance <= player.stoppingDistance + 0.1f)
                    {
                        ReachedDestination = true;
                        break;
                    }
                }
            
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (stopAtEnd || !ReachedDestination)
            {
                player.ResetPath();
                player.isStopped = true;
            }
            if (!ReachedDestination)
            {
                Debug.LogWarning("Menu walk blocked. Move the markers onto a connected NavMesh and rebake.", this);
            }
        }
    }
}
