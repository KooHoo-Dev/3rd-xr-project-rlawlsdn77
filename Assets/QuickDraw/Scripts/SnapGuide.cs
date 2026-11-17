using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 손이 가까이 오면(hover) 잡혔을 때 붙을 자리에 반투명 미리보기를 보여준다. 잡거나 손이 멀어지면 숨긴다.
    // 미리보기는 이 오브젝트와 자식의 메시를 guideMaterial로 복제한 것이고, 손의 Attach에 붙어 손을 그대로 따라간다.
    [RequireComponent(typeof(XRGrabInteractable))]
    public class SnapGuide : MonoBehaviour
    {
        [Header("미리보기")]
        [SerializeField] private Material guideMaterial;    // 반투명 머티리얼 (M_SnapGuide)

        private XRGrabInteractable grab;
        private Transform preview;
        private Vector3 previewLocalPosition;       // 손 Attach 기준 미리보기 위치
        private Quaternion previewLocalRotation;    // 손 Attach 기준 미리보기 회전

        #region Unity Methods

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            preview = CreatePreview();
            preview.gameObject.SetActive(false);

            // XRI는 이 물체의 Attach(GunAttach 등)를 손의 Attach에 맞춘다.
            // 그러므로 손 Attach 기준 물체의 자세 = (물체 기준 Attach 자세)의 역
            Transform attach = grab.attachTransform != null ? grab.attachTransform : transform;
            Quaternion attachRotation = Quaternion.Inverse(transform.rotation) * attach.rotation;
            previewLocalRotation = Quaternion.Inverse(attachRotation);
            previewLocalPosition = previewLocalRotation * -transform.InverseTransformPoint(attach.position);
        }

        private void LateUpdate()
        {
            // 잡을 수 있는 손이 가까이 있고 아직 잡히지 않았을 때만 보인다 (반대 손은 Interaction Layer 때문에 hover도 안 됨)
            bool show = grab.isHovered && !grab.isSelected;
            if (preview.gameObject.activeSelf != show) preview.gameObject.SetActive(show);   // 바뀔 때만 (탄창마다 매 프레임 부르지 않게)
            if (!show) return;

            Transform handAttach = grab.interactorsHovering[0].GetAttachTransform(grab);
            if (preview.parent == handAttach) return;

            preview.SetParent(handAttach, false);
            preview.SetLocalPositionAndRotation(previewLocalPosition, previewLocalRotation);
        }

        private void OnDisable()
        {
            if (preview != null) preview.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (preview != null) Destroy(preview.gameObject);
        }

        #endregion

        // 이 오브젝트와 자식의 메시를 같은 상대 위치에 복제하고 guideMaterial을 씌운다 (Magazine도 끼울 자리 가이드로 하나 더 만듦)
        public Transform CreatePreview()
        {
            Transform root = new GameObject($"{name} SnapGuide").transform;
            root.localScale = transform.lossyScale;

            foreach (MeshFilter source in GetComponentsInChildren<MeshFilter>())
            {
                // 글자(UI_Gun의 남은 탄 숫자)는 실행 중에 메시가 만들어지고 물체 모양도 아니므로 뺀다
                if (source.sharedMesh == null || source.TryGetComponent(out TMP_Text _)) continue;

                Matrix4x4 relative = transform.worldToLocalMatrix * source.transform.localToWorldMatrix;

                GameObject part = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(root, false);
                part.transform.SetLocalPositionAndRotation(relative.GetPosition(), relative.rotation);
                part.transform.localScale = relative.lossyScale;
                part.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;

                Material[] materials = new Material[source.sharedMesh.subMeshCount];
                System.Array.Fill(materials, guideMaterial);
                MeshRenderer partRenderer = part.GetComponent<MeshRenderer>();
                partRenderer.sharedMaterials = materials;
                partRenderer.shadowCastingMode = ShadowCastingMode.Off;
                partRenderer.receiveShadows = false;
            }
            return root;
        }
    }
}
