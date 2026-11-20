using System;
using System.Collections;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 표적. 루트 피벗은 아래 끝(쓰러지는 축)이고 원판은 자식 Face(원판 모양 콜라이더). PopupPattern이 Show / Hide / Fall로 부린다.
    public class Target : MonoBehaviour, IShootable
    {
        // 서 있는 동안의 움직임
        public enum Motion { None, Horizontal, Vertical, Diagonal, Approach }

        [Header("표적")]
        [SerializeField] private float riseTime = 0.2f;     // 일어나기·쓰러지기에 걸리는 시간 (초)
        [SerializeField] private ParticleSystem hitEffect;  // 맞은 자리에 만드는 효과 (HitEffect.prefab, 끝나면 스스로 사라짐)

        [Header("이동 (Horizontal / Vertical / Diagonal)")]
        [SerializeField] private Vector2 moveSpeedRange = new Vector2(1f, 2.5f);    // 표적마다 고르는 속도 (m/s, 최소~최대)
        [SerializeField] private Vector2 moveWidthRange = new Vector2(1f, 3f);      // 표적마다 고르는 왕복 폭 (m, 최소~최대). 자리를 가운데로 양옆 절반씩

        [Header("다가오기 (Approach)")]
        [SerializeField] private float approachSpeed = 3f;  // 플레이어 쪽으로 다가오는 속도 (m/s)

        public bool IsStanding { get; private set; }        // 서 있어 맞힐 수 있음 (쓰러지는 중이면 false)
        public Motion CurrentMotion { get; private set; }
        public event Action<Target, Vector3> Hit;           // 맞음 (맞은 지점). 맞은 표적은 바로 사라진 뒤

        private Quaternion facing;      // 서 있을 때 회전 (+Z = 플레이어 쪽)
        private Coroutine motion;
        private Vector3 basePosition;   // 움직임의 가운데 (루트 위치)
        private Vector3 moveDirection;  // 왕복 방향 (단위 벡터)
        private float moveSpeed;
        private float moveWidth;
        private float shownAt;

        // 원판 중심이 center에 오도록, 반지름 radius로 일으켜 세운다. 표적 면은 플레이어(원점) 쪽을 본다
        // 이동 표적은 maxWidth를 넘지 않는 폭으로 자리를 가운데로 왕복하고(PopupPattern이 높이 범위에 맞춰 넘김), 다가오는 표적은 플레이어 쪽으로 온다
        public void Show(Vector3 center, float radius, Motion targetMotion, float maxWidth)
        {
            Vector3 toPlayer = -center;
            toPlayer.y = 0f;
            facing = Quaternion.LookRotation(toPlayer);
            transform.localScale = Vector3.one * (radius * 2f);
            basePosition = center - Vector3.up * radius;        // 루트 = 원판 아래 끝
            transform.position = basePosition;
            moveSpeed = UnityEngine.Random.Range(moveSpeedRange.x, moveSpeedRange.y);
            moveWidth = Mathf.Min(UnityEngine.Random.Range(moveWidthRange.x, moveWidthRange.y), maxWidth);
            // 높이 범위 끝에 붙어 위아래로 오갈 폭이 없으면 제자리에 선다
            CurrentMotion = targetMotion != Motion.Approach && moveWidth <= 0f ? Motion.None : targetMotion;
            moveDirection = DirectionOf(targetMotion);
            shownAt = Time.time;

            gameObject.SetActive(true);
            IsStanding = true;
            Rotate(-90f, 0f, false);
        }

        // 바로 사라진다 (맞았을 때)
        public void Hide()
        {
            IsStanding = false;
            if (motion != null) StopCoroutine(motion);
            motion = null;
            gameObject.SetActive(false);
        }

        // 뒤로 쓰러진 뒤 사라진다 (서 있는 시간이 다 됐거나, 다가와서 가까워졌거나, 게임이 끝났을 때)
        public void Fall()
        {
            if (!IsStanding) return;
            IsStanding = false;
            Rotate(0f, -90f, true);
        }

        public void Shooted(RaycastHit hit)
        {
            if (!IsStanding) return;
            Debug.Log($"[Target] 명중: {name}");
            // 맞은 면에서 플레이어 쪽(hit.normal)으로 튀게 세운다
            if (hitEffect != null) Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Hide();
            Hit?.Invoke(this, hit.point);
        }

        #region Unity Methods

        // 서 있는 동안 움직인다. 쓰러지기 시작하면 그 자리에 멈춘다
        private void Update()
        {
            if (!IsStanding || CurrentMotion == Motion.None) return;

            if (CurrentMotion == Motion.Approach)
            {
                transform.position += facing * Vector3.forward * (approachSpeed * Time.deltaTime);
                return;
            }

            // 가운데에서 출발해 moveDirection으로 왕복한다
            float offset = Mathf.PingPong((Time.time - shownAt) * moveSpeed + moveWidth / 2f, moveWidth) - moveWidth / 2f;
            transform.position = basePosition + moveDirection * offset;
        }

        #endregion

        // 표적 면 기준 왕복 방향 (좌우 = 면의 옆, 위아래 = 위, 대각선 = 그 사이 45°)
        private Vector3 DirectionOf(Motion targetMotion)
        {
            Vector3 right = facing * Vector3.right;
            if (targetMotion == Motion.Horizontal) return right;
            if (targetMotion == Motion.Vertical) return Vector3.up;
            if (targetMotion == Motion.Diagonal) return (right + Vector3.up).normalized;
            return Vector3.zero;
        }

        // 루트(아래 끝)를 축으로 from → to 각도(X축, 음수 = 플레이어 반대쪽으로 누움)로 돌린다
        private void Rotate(float from, float to, bool hideAtEnd)
        {
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(RotateRoutine(from, to, hideAtEnd));
        }

        private IEnumerator RotateRoutine(float from, float to, bool hideAtEnd)
        {
            for (float t = 0f; t < riseTime; t += Time.deltaTime)
            {
                transform.rotation = facing * Quaternion.Euler(Mathf.Lerp(from, to, t / riseTime), 0f, 0f);
                yield return null;
            }
            transform.rotation = facing * Quaternion.Euler(to, 0f, 0f);
            motion = null;
            if (hideAtEnd) gameObject.SetActive(false);
        }
    }
}
