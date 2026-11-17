using UnityEngine;

namespace QuickDraw.Scripts
{
    public class Gun : MonoBehaviour
    {
        [Header("총 규격")]
        [SerializeField] private float effectRange = 30f;   // 총 최대 사거리 (가장 먼 표적 15m보다 길게)

        [Header("레이 캐스트")]
        [SerializeField] private Vector3 muzzleOffset;      // 총구 위치 (총 기준 로컬 좌표)
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;   // Default + Shootable만 체크

        // 총구가 겨누고 있는 오브젝트 (표적 / UI 포함)
        public GameObject AimTarget { get; private set; }

        private Vector3 MuzzleWorldPos => transform.TransformPoint(muzzleOffset);

        // PlayerController가 호출한다. 겨누고 있는 대상이 맞을 수 있는 물체면 맞힌다
        public void TryFire()
        {
            Debug.Log("[Gun] 발사");
            if (AimTarget != null && AimTarget.TryGetComponent(out IShootable shootable))
                shootable.Shooted();
        }

        // PlayerController가 호출한다. 탄창 소켓이 생기기 전이라 로그만 남긴다
        public void EjectMagazine()
        {
            Debug.Log("[Gun] 탄창 제거");
        }

        private void Update()
        {
            GameObject aimTarget = null;
            if (Physics.Raycast(MuzzleWorldPos, transform.right, out RaycastHit hit, effectRange, hitMask))
                aimTarget = hit.transform.gameObject;

            // 대상이 바뀔 때만 로그를 남긴다
            if (aimTarget == AimTarget) return;
            AimTarget = aimTarget;
            Debug.Log($"[Gun] 조준 대상: {(AimTarget != null ? AimTarget.name : "없음")}");
        }

        // 총구 위치와 사거리 표시 (모델의 총열 = 로컬 +X)
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(MuzzleWorldPos, 0.015f);
            Gizmos.DrawLine(MuzzleWorldPos, MuzzleWorldPos + transform.right * effectRange);
        }
    }
}
