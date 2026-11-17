using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 테이블의 탄창 자리마다 탄창을 놓고, 집어 가면 respawnDelay 뒤 그 자리에 새 탄창을 놓는다 (탄창은 몇 개든 가져갈 수 있음)
    public class MagazineSpawner : MonoBehaviour
    {
        [Header("탄창 보충")]
        [SerializeField] private XRGrabInteractable magazinePrefab;    // Magazine.prefab
        [SerializeField] private Transform[] slots;                     // 탄창 자리 (자식 Slot1, Slot2 …)
        [SerializeField] private float respawnDelay = 0.3f;             // 집어 간 뒤 새 탄창이 놓이기까지 (초)

        #region Unity Methods

        private void Start()
        {
            foreach (Transform slot in slots) Spawn(slot);
        }

        #endregion

        private void Spawn(Transform slot)
        {
            XRGrabInteractable magazine = Instantiate(magazinePrefab, slot.position, slot.rotation);
            magazine.name = magazinePrefab.name;

            // 이 탄창이 처음 집히면 그 자리를 다시 채운다
            void OnPicked(SelectEnterEventArgs args)
            {
                magazine.selectEntered.RemoveListener(OnPicked);
                StartCoroutine(SpawnAfterDelay(slot));
            }
            magazine.selectEntered.AddListener(OnPicked);
        }

        private IEnumerator SpawnAfterDelay(Transform slot)
        {
            yield return new WaitForSeconds(respawnDelay);
            Spawn(slot);
        }
    }
}
