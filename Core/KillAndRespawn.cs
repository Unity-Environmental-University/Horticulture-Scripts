using UnityEngine;

namespace _project.Scripts.Core
{
    public class KillAndRespawn : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        private GameObject _player;
        private Vector3 _playerStartPos;

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _playerStartPos = _player.transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject != _player) return;
            Destroy(_player);
            _player = Instantiate(playerPrefab, _playerStartPos, Quaternion.identity);
        }
    }
}