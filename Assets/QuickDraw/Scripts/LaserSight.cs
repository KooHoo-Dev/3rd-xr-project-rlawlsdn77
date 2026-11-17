using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 총을 잡고 있을 때만 총구에서 조준선과 조준점을 보여준다. Gun과 같은 오브젝트에 붙인다.
    // 선과 점을 총 로컬 좌표로 그려서, XRI가 렌더링 직전에 총을 옮겨도 총과 어긋나지 않는다.
    [RequireComponent(typeof(Gun), typeof(LineRenderer))]
    public class LaserSight : MonoBehaviour
    {
        [Header("레이저")]
        [SerializeField] private Material material;         // 선과 점 머티리얼 (M_Laser, 빨강. 색은 머티리얼에서 바꿈)
        [SerializeField] private float width = 0.003f;      // 선 굵기 (m)
        [SerializeField] private float dotScale = 0.005f;   // 거리 1m당 점 지름 (멀어도 비슷한 크기로 보이게)

        private Gun gun;
        private XRGrabInteractable grab;
        private LineRenderer line;
        private Transform dot;

        #region Unity Methods

        private void Awake()
        {
            gun = GetComponent<Gun>();
            grab = GetComponent<XRGrabInteractable>();
            line = GetComponent<LineRenderer>();

            line.useWorldSpace = false;
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
        }

        private void Start()
        {
            // SnapGuide가 Awake에서 총 메시를 복제하므로, 점은 Start에서 만들어 미리보기에 섞이지 않게 한다
            GameObject dotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(dotObject.GetComponent<Collider>());   // 조준 레이캐스트와 손에 걸리지 않게
            dotObject.name = "LaserDot";

            MeshRenderer dotRenderer = dotObject.GetComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = material;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;

            dot = dotObject.transform;
            dot.SetParent(transform, false);
            dotObject.SetActive(false);
        }

        private void LateUpdate()
        {
            bool held = grab.isSelected;
            line.enabled = held;
            dot.gameObject.SetActive(held && gun.AimTarget != null);
            if (!held) return;

            // 총열 = 로컬 +X. 총 스케일이 1이라 로컬 거리 = 월드 거리
            Vector3 end = gun.MuzzleOffset + Vector3.right * gun.AimDistance;
            line.SetPosition(0, gun.MuzzleOffset);
            line.SetPosition(1, end);
            dot.localPosition = end;
            dot.localScale = Vector3.one * (dotScale * gun.AimDistance);
        }

        #endregion
    }
}
