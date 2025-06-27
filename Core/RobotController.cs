using UnityEngine;
using UnityEngine.AI;

namespace _project.Scripts.Core
{
    public class RobotController : MonoBehaviour
    {
        [Header("Robot Stuff")]
        public Transform robotTransform;
        public GameObject currentLookTarget;
        public NavMeshAgent agent;
        public Animator animator;
    
        private void Update()
        {
            if (!currentLookTarget) return;
            LookAtTarget();
        }

        private void LookAtTarget()
        {
            var direction = currentLookTarget.transform.position - robotTransform.position;
            direction.y = 0;
            var targetRotation = Quaternion.LookRotation(direction);
            robotTransform.rotation = Quaternion.Slerp(robotTransform.rotation, targetRotation, Time.deltaTime * 10);
        }

        public void SetNewFocusTarget(GameObject newFocusTarget) => currentLookTarget = newFocusTarget;

        public void GoToNewLocation(Vector3 newPosition)
        {
            if(agent.transform.position == newPosition) return;
            agent.SetDestination(newPosition);
        }
    
        public bool HasReachedDestination() => agent.remainingDistance <= agent.stoppingDistance;
    }
}