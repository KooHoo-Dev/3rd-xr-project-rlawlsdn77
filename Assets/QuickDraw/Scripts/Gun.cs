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
        [SerializeField] private ParticleSystem muzzleFlash;    // 총구 화염 (총의 자식 MuzzleFlash, 비어 있으면 없음)
        [SerializeField] private AudioClip gunfireClip;         // 총소리 (비어 있으면 소리 없이 쏨)
        [SerializeField] private AudioClip dryFireClip;         // 탄이 없을 때 딸깍 (비어 있으면 소리 없음)
        [SerializeField] private AudioClip magazineInClip;      // 탄창을 끼울 때 (비어 있으면 소리 없음)
        [SerializeField] private AudioClip magazineOutClip;     // 탄창이 빠질 때 (비어 있으면 소리 없음)
        [SerializeField] private float hapticAmplitude = 0.7f;  // 진동 세기 (0~1)
        [SerializeField] private float hapticDuration = 0.08f;  // 진동 시간 (초)

        [Header("겉모습 (이스터에그)")]
        [SerializeField] private Renderer[] bodyRenderers;      // 겉모습을 바꿀 몸체 (Gun, Gate_07, Trigger_Guard_07)

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
        private Material[] originalMaterials;   // 몸체의 원래 머티리얼 (SetSkin(null)로 되돌림)
        private Magazine magazine;      // 꽂혀 있는 탄창
        private int bonusCapacity;      // 이번 판 콤보로 늘어난 탄 수 (끼우는 탄창에 더함)

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
            if (muzzleFlash != null) muzzleFlash.Play();
            SendHaptic();
            // 맞히기보다 먼저 알린다. 그래서 쏜 탄은 맞힌 대상이 단계를 바꾸기 전의 단계에서 세어지고,
            // GameManager는 이 탄이 표적에 맞았는지(TargetHit가 뒤따르는지)로 빗나감을 안다
            Fired?.Invoke();

            // 지금 겨눈 대상이나 그 부모가 맞을 수 있는 물체면 맞힌다
            // (AimTarget은 Update에서 정해서 이 호출보다 한 프레임 늦을 수 있으므로 여기서 다시 겨눈다)
            // (콜라이더는 자식 Face에, IShootable은 루트에 붙을 수 있다)
            if (Aim(out RaycastHit hit))
            {
                IShootable shootable = hit.collider.GetComponentInParent<IShootable>();
                if (shootable != null) shootable.Shooted(hit);
            }
            return true;
        }

        // 끼울 자리 근처에서 탄창을 놓았을 때 Magazine이 부른다. 끼우면 바로 쏠 수 있다
        public void InsertMagazine(Magazine inserted)
        {
            if (HasMagazine) return;
            magazine = inserted;
            inserted.AttachTo(magazineSocket);
            SetAmmo(magazineCapacity + bonusCapacity);
            PlayClip(magazineInClip);
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
            PlayClip(magazineOutClip);
            Debug.Log("[Gun] 탄창 제거");
        }

        // GameManager(콤보 보너스)가 호출한다. 지금 꽂힌 탄창에 바로 amount발을 더하고, 이 판에 앞으로 끼울 탄창도 amount발 늘린다
        public void AddCapacity(int amount)
        {
            bonusCapacity += amount;
            if (HasMagazine) SetAmmo(Ammo + amount);
            Debug.Log($"[Gun] 콤보 보너스 +{amount}발 (탄창 {magazineCapacity + bonusCapacity}발)");
        }

        // GameManager(새 판 시작)가 호출한다. 앞으로 끼울 탄창을 기본 발수로 되돌린다 (지금 탄은 그대로)
        public void ResetCapacity()
        {
            bonusCapacity = 0;
        }

        // UI(튜토리얼 이스터에그)가 호출한다. 몸체를 skin으로 칠하고, null이면 원래 머티리얼로 되돌린다
        public void SetSkin(Material skin)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
                bodyRenderers[i].sharedMaterial = skin != null ? skin : originalMaterials[i];
        }

        #region Unity Methods

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            originalMaterials = new Material[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++) originalMaterials[i] = bodyRenderers[i].sharedMaterial;
        }

        private void Update()
        {
            GameObject aimTarget = null;
            AimDistance = effectRange;
            // hit.transform은 Rigidbody가 있으면 그 오브젝트를 돌려주므로 맞은 콜라이더를 쓴다
            if (Aim(out RaycastHit hit))
            {
                aimTarget = hit.collider.gameObject;
                AimDistance = hit.distance;
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

        // 총구에서 총열 방향으로 레이캐스트한다 (Update의 조준선과 TryFire의 명중 판정이 같이 씀)
        // 쥐고 있을 때만 조준한다 (내려놓은 총은 쏠 수도, 레이저를 그릴 수도 없다). 트리거 콜라이더는 무시한다
        private bool Aim(out RaycastHit hit)
        {
            hit = default;
            return grab.isSelected &&
                Physics.Raycast(MuzzleWorldPos, transform.right, out hit, effectRange, hitMask, QueryTriggerInteraction.Ignore);
        }

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
