using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 탄창. 손에 든 채로 빈 총의 끼울 자리(MagazineSocket) 근처(insertDistance)에 오면 그 자리에 반투명 탄창 가이드가 보이고,
    // 그때 손을 놓으면 Gun.InsertMagazine(this)로 총에 끼워진다. 다른 곳에서 놓으면 떨어져 바닥에 남는다 (다시 주울 수 있음).
    // 끼운 탄창은 총의 자식으로 붙어 따라가고 잡을 수 없다. 총에서 빼면 떨어지고 droppedLifetime 뒤 사라진다 (다시 못 잡음).
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody), typeof(SnapGuide))]
    public class Magazine : MonoBehaviour
    {
        [Header("끼우기")]
        [SerializeField] private float insertDistance = 0.1f;   // 끼울 자리에서 이만큼 안에 오면 가이드가 보이고, 놓으면 끼워짐 (m)

        [Header("뺀 탄창")]
        [SerializeField] private float droppedLifetime = 5f;    // 총에서 빼낸 뒤 사라지기까지 (초)

        // 씬의 총. 탄창마다 씬을 뒤지지 않도록 모든 탄창이 같이 쓰고, 씬이 바뀌어 파괴되면 다시 찾는다
        private static Gun sceneGun;
        private static Gun SceneGun => sceneGun != null ? sceneGun : (sceneGun = FindFirstObjectByType<Gun>());

        private XRGrabInteractable grab;
        private Rigidbody body;
        private Transform insertGuide;      // 총에 끼워질 자리에 보이는 반투명 탄창 (총의 끼울 자리에 붙음)
        private bool insertRequested;

        // Gun.InsertMagazine이 부른다. 끼울 자리에 붙여 총을 따라가게 하고, 손으로 다시 잡지 못하게 한다
        public void AttachTo(Transform socket)
        {
            grab.enabled = false;
            body.isKinematic = true;
            transform.SetParent(socket, false);     // 탄창 모델은 총과 같은 좌표계라, 회전 없이 붙이면 손잡이에 끼운 모습이다
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        // Gun.EjectMagazine이 부른다. 총에서 떨어뜨리고, droppedLifetime 뒤 지운다 (잡기는 꺼진 그대로라 다시 꽂아 10발 채우기 불가, 남은 탄도 같이 버려짐)
        public void Drop()
        {
            transform.SetParent(null, true);
            body.isKinematic = false;
            Destroy(gameObject, droppedLifetime);
            Debug.Log($"[Magazine] 뺀 탄창: {droppedLifetime}초 뒤 제거");
        }

        #region Unity Methods

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            insertGuide = GetComponent<SnapGuide>().CreatePreview();
            insertGuide.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            grab.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            grab.selectExited.RemoveListener(OnReleased);
        }

        private void Update()
        {
            // 손에 들고 빈 총의 끼울 자리 근처에 있을 때만 가이드가 보인다
            bool show = grab.isSelected && NearEmptyGun();
            if (insertGuide.gameObject.activeSelf != show)
            {
                if (show) insertGuide.SetParent(SceneGun.MagazineSocket, false);
                insertGuide.gameObject.SetActive(show);
            }

            // 끼우기는 선택 이벤트 안이 아니라 여기서 한다 (XRI가 놓기 처리를 끝낸 뒤)
            if (!insertRequested) return;
            insertRequested = false;
            if (NearEmptyGun()) SceneGun.InsertMagazine(this);
        }

        private void OnDestroy()
        {
            if (insertGuide != null) Destroy(insertGuide.gameObject);  // 가이드는 총에 붙어 있으므로 따로 지운다
        }

        #endregion

        // 총에 탄창이 없고, 이 탄창이 끼울 자리 근처에 있음
        private bool NearEmptyGun()
        {
            Gun gun = SceneGun;
            return gun != null && !gun.HasMagazine
                && Vector3.Distance(transform.position, gun.MagazineSocket.position) <= insertDistance;
        }

        // 가이드가 보이는 자리에서 놓았으면 다음 Update에서 끼운다
        private void OnReleased(SelectExitEventArgs args)
        {
            if (!grab.isSelected) insertRequested = NearEmptyGun();
        }
    }
}
