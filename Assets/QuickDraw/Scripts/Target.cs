using System;
using System.Collections;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 표적. 루트 피벗은 아래 끝(쓰러지는 축)이고 원판은 자식 Face(원판 모양 콜라이더). PopupPattern이 Show / Hide / Fall로 부린다.
    public class Target : MonoBehaviour, IShootable
    {
        [Header("표적")]
        [SerializeField] private float riseTime = 0.2f;     // 일어나기·쓰러지기에 걸리는 시간 (초)

        public bool IsStanding { get; private set; }        // 서 있어 맞힐 수 있음 (쓰러지는 중이면 false)
        public event Action<Target> Hit;                    // 맞음 (맞은 표적은 바로 사라진 뒤)

        private Quaternion facing;      // 서 있을 때 회전 (+Z = 플레이어 쪽)
        private Coroutine motion;

        // 원판 중심이 center에 오도록, 반지름 radius로 일으켜 세운다. 표적 면은 플레이어(원점) 쪽을 본다
        public void Show(Vector3 center, float radius)
        {
            Vector3 toPlayer = -center;
            toPlayer.y = 0f;
            facing = Quaternion.LookRotation(toPlayer);
            transform.localScale = Vector3.one * (radius * 2f);
            transform.position = center - Vector3.up * radius;  // 루트 = 원판 아래 끝

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

        // 뒤로 쓰러진 뒤 사라진다 (서 있는 시간이 다 됐거나 게임이 끝났을 때)
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
            Hide();
            Hit?.Invoke(this);
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
