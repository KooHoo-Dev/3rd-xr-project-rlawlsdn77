using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace QuickDraw.Scripts
{
    // 손에서 놓친 뒤 returnDelay 안에 다시 잡지 않으면 처음 놓여 있던 자리(테이블)로 돌려놓는다
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class ReturnToSpawn : MonoBehaviour
    {
        [Header("복귀")]
        [SerializeField] private float returnDelay = 2f;    // 놓친 뒤 돌아가기까지 시간 (초)

        private XRGrabInteractable grab;
        private Rigidbody body;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Coroutine returning;

        #region Unity Methods

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            transform.GetPositionAndRotation(out spawnPosition, out spawnRotation);
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
        }

        #endregion

        // 다시 잡으면 복귀를 취소한다
        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (returning == null) return;
            StopCoroutine(returning);
            returning = null;
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            // 잡힌 채로 꺼지거나 씬이 바뀌면 XRI가 놓기를 보내는데, 꺼지는 중인 오브젝트는 코루틴을 못 돌린다
            // (이때 isActiveAndEnabled는 아직 true일 수 있어 activeInHierarchy로 본다)
            if (grab.isSelected || !gameObject.activeInHierarchy || !enabled) return;
            returning = StartCoroutine(ReturnAfterDelay());
        }

        private IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(returnDelay);
            returning = null;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            Debug.Log($"[ReturnToSpawn] {name} 제자리로 복귀");
        }
    }
}
