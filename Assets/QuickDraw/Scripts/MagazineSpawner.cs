using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 테이블의 탄창 자리마다 탄창을 놓고, 그 탄창이 집혀 가거나 밀려나거나 사라지면 respawnDelay 뒤 그 자리에 새 탄창을 놓는다 (탄창은 몇 개든 가져갈 수 있음)
    public class MagazineSpawner : MonoBehaviour
    {
        [Header("탄창 보충")]
        [SerializeField] private XRGrabInteractable magazinePrefab;    // Magazine.prefab
        [SerializeField] private Transform[] slots;                     // 탄창 자리 (자식 Slot1, Slot2 …)
        [SerializeField] private float respawnDelay = 0.3f;             // 자리가 빈 뒤 새 탄창이 놓이기까지 (초)
        [SerializeField] private float leaveDistance = 0.05f;           // 놓은 탄창이 자리에서 수평으로 이만큼 넘게 벗어나면 빈 자리로 봄 (m)

        private XRGrabInteractable[] placed;    // 자리마다 놓은 탄창
        private float[] emptyTime;              // 자리가 빈 채로 지난 시간 (초)

        #region Unity Methods

        private void Start()
        {
            placed = new XRGrabInteractable[slots.Length];
            emptyTime = new float[slots.Length];
            for (int i = 0; i < slots.Length; i++) Spawn(i);
        }

        private void Update()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (IsInSlot(i))
                {
                    emptyTime[i] = 0f;
                    continue;
                }
                emptyTime[i] += Time.deltaTime;
                if (emptyTime[i] >= respawnDelay) Spawn(i);
            }
        }

        #endregion

        private void Spawn(int index)
        {
            placed[index] = Instantiate(magazinePrefab, slots[index].position, slots[index].rotation);
            placed[index].name = magazinePrefab.name;
            emptyTime[index] = 0f;
        }

        // 그 자리에 놓은 탄창이 아직 자리에 있음 (집혀 가거나, 밀려나거나, 사라졌으면 false)
        // 자리는 테이블보다 조금 위라 탄창이 떨어져 놓이므로, 높이는 빼고 수평 거리만 본다
        private bool IsInSlot(int index)
        {
            XRGrabInteractable magazine = placed[index];
            if (magazine == null || magazine.isSelected) return false;

            Vector3 offset = magazine.transform.position - slots[index].position;
            offset.y = 0f;
            return offset.magnitude <= leaveDistance;
        }
    }
}
