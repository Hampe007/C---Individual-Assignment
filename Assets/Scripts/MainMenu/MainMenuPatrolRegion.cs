using UnityEngine;
using UnityEngine.AI;

namespace GameMenus
{
    public sealed class MainMenuPatrolRegion : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(12f, 16f);
        [SerializeField] private NavMeshAgent[] enemies;
        [SerializeField] private Color color = new(0.8f, 0.15f, 0.2f, 0.18f);
        private NavMeshPath path;

        public bool Includes(NavMeshAgent agent) => isActiveAndEnabled && enemies != null && System.Array.IndexOf(enemies, agent) >= 0;

        public bool Contains(Vector3 position, float clearance = 0f)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            return Mathf.Abs(local.x) <= size.x * 0.5f - clearance && Mathf.Abs(local.z) <= size.y * 0.5f - clearance;
        }

        public bool TryPoint(NavMeshAgent agent, bool starting, out Vector3 point)
        {
            path ??= new NavMeshPath();
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            float clearance = Mathf.Min(Mathf.Min(size.x, size.y) * 0.25f, agent.radius + 0.75f);
            Vector2 insetSize = size - Vector2.one * clearance * 2f;
            Vector3 origin = agent.transform.position;
            if (starting)
            {
                if (!NavMesh.SamplePosition(transform.position, out NavMeshHit anchor, Mathf.Min(size.x, size.y) * 0.25f, filter) || !Contains(anchor.position, clearance))
                {
                    point = default;
                    return false;
                }
            
                origin = anchor.position;
            }
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector3 sample = transform.TransformPoint(new Vector3(Random.Range(-0.5f, 0.5f) * insetSize.x, 0f, Random.Range(-0.5f, 0.5f) * insetSize.y));
                if (!NavMesh.SamplePosition(sample, out NavMeshHit hit, 1.5f, filter) || !Contains(hit.position, clearance))
                {
                    continue;
                }
            
                if (!starting && Vector3.Distance(origin, hit.position) < 1.5f || !NavMesh.CalculatePath(origin, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }
            
                bool inside = true;
                foreach (Vector3 corner in path.corners)
                {
                    inside &= Contains(corner, clearance);
                }
            
                if (!inside)
                {
                    continue;
                }
            
                point = hit.position;
                return true;
            }
            point = default;
            return false;
        }

        private void OnValidate() => size = new Vector2(Mathf.Max(2f, size.x), Mathf.Max(2f, size.y));

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = color;
            Vector3 size = new(this.size.x, 0.1f, this.size.y);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
