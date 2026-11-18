using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 표적 등장 방식: 정해진 자리 중 빈 곳에 나타났다가 lifetime 뒤 쓰러진다. 맞으면 바로 사라진다.
    // 방금 표적이 사라진 자리에는 바로 다시 나오지 않는다. 자리는 고르는 순간 예약된다.
    // 나오는 표적 중 movingChance 비율은 좌우로 움직인다. volleyInterval마다 일제 등장(volleyCount개를 한꺼번에)이 있다.
    // GameManager가 단계마다 크기·동시 수·유지 시간·이동 비율·일제 등장 간격을 정해 Begin을 부르고(서 있던 표적은 그대로), 게임이 끝나면 End를 부른다.
    public class PopupPattern : MonoBehaviour
    {
        [Header("팝업")]
        [SerializeField] private Target targetPrefab;               // Target.prefab
        [SerializeField] private Vector3[] slots =                  // 표적 원판 중심 자리 (월드 좌표, z = 정면 거리)
        {
            new Vector3(-3f, 1.2f, 6f), new Vector3(0f, 1.5f, 5f), new Vector3(3f, 1.2f, 6f),
            new Vector3(-4.5f, 1.8f, 10f), new Vector3(1.5f, 2f, 9f), new Vector3(-1.5f, 1.4f, 13f),
            new Vector3(4f, 1.6f, 14f),
        };

        [Header("일제 등장")]
        [SerializeField] private int volleyCount = 4;               // 한꺼번에 세우는 수
        [SerializeField] private float volleyLifetime = 2.5f;       // 일제 등장 표적이 서 있는 시간 (초)

        public event Action TargetHit;                              // 표적이 맞음 (GameManager가 점수에 씀)

        private readonly List<Target> targets = new List<Target>(); // 만들어 둔 표적 (필요할 때 늘려 돌려 씀)
        private readonly Dictionary<Target, int> slotOf = new Dictionary<Target, int>();
        private readonly Dictionary<Target, float> fallAt = new Dictionary<Target, float>();
        private int lastVacated = -1;   // 방금 표적이 사라진 자리
        private float radius;
        private int activeCount;        // 동시에 서 있는 최대 수
        private float lifetime;         // 나타난 뒤 쓰러지기까지 (초)
        private float movingChance;     // 좌우로 움직이는 표적의 비율 (0~1)
        private float volleyInterval;   // 일제 등장 간격 (초, 0이면 없음)
        private Coroutine spawning;

        public void Begin(float targetRadius, int count, float targetLifetime, float targetMovingChance, float targetVolleyInterval)
        {
            radius = targetRadius;
            activeCount = count;
            lifetime = targetLifetime;
            movingChance = targetMovingChance;
            volleyInterval = targetVolleyInterval;
            if (spawning != null) StopCoroutine(spawning);
            spawning = StartCoroutine(SpawnLoop());
        }

        // 게임이 끝나면 서 있는 표적이 한꺼번에 쓰러진다
        public void End()
        {
            if (spawning != null) StopCoroutine(spawning);
            spawning = null;
            foreach (Target target in targets) target.Fall();
        }

        #region Unity Methods

        private void Update()
        {
            foreach (Target target in targets)
            {
                if (!target.IsStanding || Time.time < fallAt[target]) continue;
                lastVacated = slotOf[target];
                target.Fall();
            }
        }

        #endregion

        // 하나씩 lifetime ÷ activeCount 간격으로 내보낸다. 그러면 안 맞혀도 activeCount개쯤이 엇갈려 서 있다
        // (간격이 고정이면 lifetime이 짧은 단계일수록 동시에 서 있는 수가 줄어든다)
        // 일제 등장 차례가 되면 새 표적을 멈춰 표적이 다 사라지길 기다린 뒤, volleyCount개를 한꺼번에 세운다
        private IEnumerator SpawnLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(lifetime / activeCount);
            float nextVolley = Time.time + volleyInterval;
            while (true)
            {
                if (volleyInterval > 0f && Time.time >= nextVolley)
                {
                    yield return new WaitUntil(NoTargetShown);
                    for (int i = 0; i < volleyCount; i++) Spawn(volleyLifetime);
                    yield return new WaitForSeconds(volleyLifetime);
                    nextVolley = Time.time + volleyInterval;
                }

                int standing = 0;
                foreach (Target target in targets)
                    if (target.IsStanding) standing++;
                if (standing < activeCount) Spawn(lifetime);
                yield return wait;
            }
        }

        private void Spawn(float standTime)
        {
            List<int> free = new List<int>();
            for (int slot = 0; slot < slots.Length; slot++)
                if (slot != lastVacated && !IsOccupied(slot)) free.Add(slot);
            if (free.Count == 0) return;

            Target target = IdleTarget();
            int chosen = free[UnityEngine.Random.Range(0, free.Count)];
            slotOf[target] = chosen;
            fallAt[target] = Time.time + standTime;
            target.Show(slots[chosen], radius, UnityEngine.Random.value < movingChance);
        }

        // 서 있거나 쓰러지는 중인 표적이 하나도 없음
        private bool NoTargetShown()
        {
            foreach (Target target in targets)
                if (target.gameObject.activeSelf) return false;
            return true;
        }

        // 쉬고 있는(꺼진) 표적. 모두 쓰고 있으면 하나 더 만든다
        private Target IdleTarget()
        {
            foreach (Target target in targets)
                if (!target.gameObject.activeSelf) return target;

            Target created = Instantiate(targetPrefab, transform);
            created.name = $"{targetPrefab.name} {targets.Count + 1}";
            created.Hit += OnTargetHit;
            targets.Add(created);
            return created;
        }

        // 서 있거나 쓰러지는 중인 표적이 있는 자리
        private bool IsOccupied(int slot)
        {
            foreach (KeyValuePair<Target, int> pair in slotOf)
                if (pair.Value == slot && pair.Key.gameObject.activeSelf) return true;
            return false;
        }

        private void OnTargetHit(Target target)
        {
            lastVacated = slotOf[target];
            TargetHit?.Invoke();
        }
    }
}
