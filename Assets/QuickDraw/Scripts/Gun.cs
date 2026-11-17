using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace QuickDraw.Scripts
{
    public class Gun : MonoBehaviour
    {
        [Header("총 규격")]
        [SerializeField] private float effectRange = 30f;   // 총 최대 사거리 (가장 먼 표적 15m보다 길게)
        [SerializeField] private int magazineCapacity = 10; // 탄창 1개 탄 수

        [Header("레이 캐스트")]
        [SerializeField] private Vector3 muzzleOffset;      // 총구 위치 (총 기준 로컬 좌표)
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;   // Default + Shootable만 체크

        [Header("탄창")]
        [SerializeField] private Transform magazineSocket;          // 끼운 탄창이 붙는 자리 (총 원점, 회전 없음)

        [Header("발사 효과")]
        [SerializeField] private AudioClip gunfireClip;         // 총소리 (비어 있으면 소리 없이 쏨)
        [SerializeField] private AudioClip dryFireClip;         // 탄이 없을 때 딸깍 (비어 있으면 소리 없음)
        [SerializeField] private float hapticAmplitude = 0.7f;  // 진동 세기 (0~1)
        [SerializeField] private float hapticDuration = 0.08f;  // 진동 시간 (초)

        public int Ammo { get; private set; }               // 남은 탄 (탄창이 없으면 0)
        public bool HasMagazine => magazine != null;        // 탄창이 꽂혀 있음
        public event Action<int> AmmoChanged;               // 남은 탄이 바뀜 (UI_Gun)
        public event Action Fired;                          // 실제로 탄이 나감 (GameManager가 쏜 수를 셈)
        public Transform MagazineSocket => magazineSocket;

        // 총구가 겨누고 있는 콜라이더의 오브젝트 (표적 / UI 포함)
        public GameObject AimTarget { get; private set; }
        // 총구에서 겨눈 지점까지 거리. 맞은 게 없으면 사거리
        public float AimDistance { get; private set; }
        public Vector3 MuzzleOffset => muzzleOffset;

        private XRGrabInteractable grab;
        private Magazine magazine;      // 꽂혀 있는 탄창
        private RaycastHit aimHit;      // AimTarget을 맞힌 레이캐스트 결과 (UI가 어느 버튼을 맞혔는지 볼 때 씀)

        private Vector3 MuzzleWorldPos => transform.TransformPoint(muzzleOffset);

        // PlayerController가 호출한다. 탄이 나갔으면 true
        // 탄창이 없거나 0발이면 딸깍 소리만 나고 false (쏜 수에 들어가지 않음)
        public bool TryFire()
        {
            if (Ammo == 0)
            {
                Debug.Log("[Gun] 딸깍 (탄 없음)");
                PlayClip(dryFireClip);
                return false;
            }

            Debug.Log("[Gun] 발사");
            SetAmmo(Ammo - 1);
            PlayClip(gunfireClip);
            SendHaptic();
            // 맞히기보다 먼저 알린다. 순서가 바뀌면 START를 쏜 탄이 게임이 시작된 뒤에 세어진다
            Fired?.Invoke();

            // 겨누고 있는 대상이나 그 부모가 맞을 수 있는 물체면 맞힌다
            // (콜라이더는 자식 Face에, IShootable은 루트에 붙을 수 있다)
            if (AimTarget != null)
            {
                IShootable shootable = AimTarget.GetComponentInParent<IShootable>();
                if (shootable != null) shootable.Shooted(aimHit);
            }
            return true;
        }

        // 끼울 자리 근처에서 탄창을 놓았을 때 Magazine이 부른다. 끼우면 바로 쏠 수 있다
        public void InsertMagazine(Magazine inserted)
        {
            if (HasMagazine) return;
            magazine = inserted;
            inserted.AttachTo(magazineSocket);
            SetAmmo(magazineCapacity);
            Debug.Log("[Gun] 탄창 끼움");
        }

        // PlayerController(A 버튼)가 호출한다. 탄을 다 쓴 탄창만 뺄 수 있다
        public void TryEjectMagazine()
        {
            if (!HasMagazine) return;
            if (Ammo > 0)
            {
                Debug.Log($"[Gun] 탄이 남아 있어 탄창을 뺄 수 없음 ({Ammo}발)");
                return;
            }
            EjectMagazine();
        }

        // GameManager(메뉴 복귀)가 호출한다. 탄이 남아 있어도 빼고, 남은 탄은 버린다. 뺀 탄창은 떨어진다
        public void EjectMagazine()
        {
            if (!HasMagazine) return;
            magazine.Drop();
            magazine = null;
            SetAmmo(0);
            Debug.Log("[Gun] 탄창 제거");
        }

        #region Unity Methods

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
        }

        private void Update()
        {
            GameObject aimTarget = null;
            AimDistance = effectRange;
            // 쥐고 있을 때만 조준한다 (내려놓은 총은 쏠 수도, 레이저를 그릴 수도 없다)
            // hit.transform은 Rigidbody가 있으면 그 오브젝트를 돌려주므로 맞은 콜라이더를 쓴다. 트리거 콜라이더는 무시한다
            if (grab.isSelected &&
                Physics.Raycast(MuzzleWorldPos, transform.right, out RaycastHit hit, effectRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                aimTarget = hit.collider.gameObject;
                AimDistance = hit.distance;
                aimHit = hit;
            }

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

        #endregion

        private void SetAmmo(int ammo)
        {
            Ammo = ammo;
            AmmoChanged?.Invoke(Ammo);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, MuzzleWorldPos);
        }

        // 총을 쥔 손의 컨트롤러를 진동시킨다 (HapticImpulsePlayer는 XR Origin의 컨트롤러 오브젝트에 있음)
        private void SendHaptic()
        {
            IXRSelectInteractor hand = grab.firstInteractorSelecting;
            if (hand == null) return;

            HapticImpulsePlayer haptics = hand.transform.GetComponentInParent<HapticImpulsePlayer>();
            if (haptics != null) haptics.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }
    }
}
