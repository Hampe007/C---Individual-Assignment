using UnityEngine;
using UnityEngine.AI;

namespace GameMenus
{
    public sealed class MainMenuPatrol : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [SerializeField] private NavMeshAgent[] agents;
        [SerializeField] private Animator[] animators;
        [SerializeField] private MainMenuPatrolRegion[] regions;
        [SerializeField, Min(0.1f)] private float speed = 2.8f;
        private MainMenuPatrolRegion[] assignments;
        private float[] nextMove;
        private float activity = 0.9f;
        private bool active;

        public void SetActivity(float activity) => this.activity = activity;

        public void StopPatrol()
        {
            active = false;
            foreach (NavMeshAgent agent in agents)
            {
                agent.enabled = false;
            }
        }

        public void StartPatrol()
        {
            assignments = new MainMenuPatrolRegion[agents.Length];
            nextMove = new float[agents.Length];
            for (int index = 0; index < agents.Length; index++)
            {
                NavMeshAgent agent = agents[index];
                agent.enabled = false;
                foreach (MainMenuPatrolRegion region in regions)
                {
                    if (region != null && region.Includes(agent))
                    {
                        assignments[index] = region;
                        break;
                    }
                }
            
                MainMenuPatrolRegion assigned = assignments[index];
                if (assigned == null || !assigned.TryPoint(agent, true, out Vector3 start))
                {
                    Debug.LogWarning("Assign this menu enemy to a patrol region containing baked navigation.", agent);
                    continue;
                }
            
                agent.transform.position = start;
                agent.enabled = true;
                agent.Warp(start);
                agent.speed = speed * activity;
                agent.acceleration = 8f;
                NextPoint(index);
                animators[index].speed = 1f;
            }
            active = true;
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }
            for (int index = 0; index < agents.Length; index++)
            {
                NavMeshAgent agent = agents[index];
                if (!agent.enabled || !agent.isOnNavMesh)
                {
                    continue;
                }
            
                agent.speed = Mathf.MoveTowards(agent.speed, this.speed * activity, Time.deltaTime * 3f);
                float speed = agent.velocity.magnitude;
                animators[index].SetFloat(SpeedParameter, speed);
                animators[index].speed = speed < 0.15f ? 1f : Mathf.Clamp(speed / 3f, 0.75f, 1.5f);
                if (!agent.pathPending && Time.time >= nextMove[index] && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.25f))
                {
                    NextPoint(index);
                }
            }
        }

        private void NextPoint(int index)
        {
            if (assignments[index].TryPoint(agents[index], false, out Vector3 point))
            {
                agents[index].SetDestination(point);
            }
            nextMove[index] = Time.time + Random.Range(0.5f, 1.5f);
        }
    }
}
