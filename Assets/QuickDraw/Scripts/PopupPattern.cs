using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuickDraw.Scripts
{
    // 표적 등장 방식: 단계별 등장 범위(좌우 각도 × 거리 × 높이) 안 무작위 위치에 나타났다가 서 있는 시간 뒤 작아지며 사라진다(그 동안에도 맞힐 수 있음). 맞으면 바로 사라진다.
    // 플레이어가 볼 때 서 있는 표적과 겹치지 않고 배경 물체(blockMask)에 가려지거나 묻히지 않는 곳, 방금 표적이 사라진 곳에서 avoidDistance 밖에만 나온다.
    // 맞히거나 작아지기 시작하면 refillDelay 뒤 바로 채운다. 일부는 이동하거나 다가오고, waveInterval마다 웨이브(여러 개 한꺼번에)가 더해진다.
    // GameManager가 단계마다 Begin(단계)을 부르고(서 있던 표적은 그대로), 게임이 끝나면 End를 부른다.
    public class PopupPattern : MonoBehaviour
    {
        [Header("표적")]
        [SerializeField] private Target targetPrefab;                           // Target.prefab

        [Header("단계별 (길이 = 단계 수, 게임 시간을 똑같이 나눔)")]
        [SerializeField] private float[] targetRadius = { 0.5f, 0.35f, 0.25f }; // 표적 반지름 (m)
        [SerializeField] private float[] spawnAngle = { 25f, 35f, 45f };        // 좌우 등장 범위 (정면에서 한쪽 각도, °)
        [SerializeField] private int[] targetCount = { 3, 4, 5 };               // 동시에 서 있는 최대 수 (웨이브 표적 빼고)
        [SerializeField] private float[] targetLifetime = { 1.5f, 1.2f, 1f };   // 서 있는 시간 (초, lifetimeJitter만큼 흔들림)
        [SerializeField] private float[] movingChance = { 0f, 0.4f, 0.4f };     // 이동 표적 비율 (0~1)
        [SerializeField] private float[] approachChance = { 0f, 0f, 0.2f };     // 다가오는 표적 비율 (0~1)
        [SerializeField] private int[] waveCount = { 0, 3, 5 };                 // 웨이브 때 한꺼번에 세우는 수 (0이면 웨이브 없음)
        [SerializeField] private float[] maxDistance = { 15f, 14f, 13f };       // 등장 거리 최대 (m). 표적이 작아지는 뒷단계는 조금 가깝게

        [Header("등장 범위 (모든 단계 같음)")]
        [SerializeField] private float minDistance = 5f;                        // 등장 거리 최소 (m). 다가오는 표적은 여기서 사라짐
        [SerializeField] private Vector2 heightRange = new Vector2(1f, 2.5f);   // 원판 중심 높이 (m, 최소~최대)
        [SerializeField] private Vector2 approachStart = new Vector2(12f, 15f); // 다가오는 표적이 나오는 거리 (m, 최소~최대)
        [SerializeField] private float avoidDistance = 2f;                      // 방금 표적이 사라진 곳에서 이 안에는 안 나옴 (m)
        [SerializeField] private int positionTries = 10;                        // 겹치지 않는 위치를 찾아 다시 고르는 횟수 (못 찾으면 그 차례는 건너뜀)
        [SerializeField] private LayerMask blockMask = 1;                       // 표적을 가리거나 묻으면 안 되는 배경 물체 (Default = 소품·건물)

        [Header("평소 채움")]
        [SerializeField] private Vector2 refillDelay = new Vector2(0.2f, 0.8f); // 맞히거나 작아지기 시작한 뒤 새 표적까지 (초, 최소~최대)
        [SerializeField] private float lifetimeJitter = 0.3f;                   // 서 있는 시간을 표적마다 이 비율까지 늘이거나 줄임 (0.3 = ±30%)

        [Header("웨이브")]
        [SerializeField] private float waveInterval = 10f;                      // 웨이브 간격 (초)
        [SerializeField] private float waveLifetime = 2.5f;                     // 웨이브 표적이 서 있는 시간 (초)

        public int StageCount => targetRadius.Length;
        public event Action<Vector3> TargetHit;                                 // 표적이 맞음 (맞은 지점, GameManager가 점수·콤보에 씀)

        private static readonly Target.Motion[] moveMotions = { Target.Motion.Horizontal, Target.Motion.Vertical, Target.Motion.Diagonal };
        private static readonly Color[] stageColors = { Color.green, Color.yellow, Color.red };  // 기즈모 단계별 색

        private readonly List<Target> targets = new List<Target>();             // 만들어 둔 표적 (필요할 때 늘려 돌려 씀)
        private readonly Dictionary<Target, float> fallAt = new Dictionary<Target, float>();
        private readonly HashSet<Target> waveTargets = new HashSet<Target>();   // 웨이브로 세운 표적 (평소 최대 수에 안 셈)
        private Vector3 lastVacated = Vector3.positiveInfinity;                 // 방금 표적이 사라진 곳 (원판 중심)
        private Transform viewer;                                               // 플레이어 머리 (겹침 판정 기준)
        private int stage;
        private Coroutine refilling;
        private Coroutine waving;

        public void Begin(int newStage)
        {
            stage = newStage;
            StopSpawning();
            refilling = StartCoroutine(RefillLoop());
            if (waveCount[stage] > 0) waving = StartCoroutine(WaveLoop());
        }

        // 게임이 끝나면 서 있는 표적이 한꺼번에 작아지며 사라진다
        public void End()
        {
            StopSpawning();
            foreach (Target target in targets) target.Shrink();
        }

        #region Unity Methods

        private void Start()
        {
            viewer = Camera.main.transform;
        }

        // 서 있는 시간이 다 됐거나, 다가오는 표적이 최소 거리까지 왔으면 작아지며 사라지게 한다
        private void Update()
        {
            foreach (Target target in targets)
            {
                if (!target.IsStanding) continue;
                bool done = target.CurrentMotion == Target.Motion.Approach
                    ? FlatDistance(target.transform.position) <= minDistance
                    : Time.time >= fallAt[target];
                if (!done) continue;
                lastVacated = CenterOf(target);
                target.Shrink();
            }
        }

#if UNITY_EDITOR
        // 단계별 등장 범위를 부채꼴 상자로, 먼 쪽 모서리에 그 단계 크기의 원판을 그린다 (항상 보임)
        private void OnDrawGizmos()
        {
            for (int s = 0; s < spawnAngle.Length; s++)
            {
                Handles.color = stageColors[s % stageColors.Length];
                float angle = spawnAngle[s];
                float far = maxDistance[s];
                foreach (float height in new[] { heightRange.x, heightRange.y })
                {
                    Handles.DrawLines(ArcSegments(height, minDistance, angle));
                    Handles.DrawLines(ArcSegments(height, far, angle));
                    if (approachChance[s] > 0f) Handles.DrawDottedLines(ArcSegments(height, approachStart.x, angle), 4f);
                }

                foreach (float side in new[] { -angle, angle })
                {
                    Vector3 direction = Quaternion.Euler(0f, side, 0f) * Vector3.forward;
                    Vector3 nearLow = direction * minDistance + Vector3.up * heightRange.x;
                    Vector3 farLow = direction * far + Vector3.up * heightRange.x;
                    Vector3 nearHigh = nearLow + Vector3.up * (heightRange.y - heightRange.x);
                    Vector3 farHigh = farLow + Vector3.up * (heightRange.y - heightRange.x);
                    Handles.DrawLine(nearLow, farLow);
                    Handles.DrawLine(nearHigh, farHigh);
                    Handles.DrawLine(nearLow, nearHigh);
                    Handles.DrawLine(farLow, farHigh);
                    Handles.DrawWireDisc(farLow, -direction, targetRadius[s]);
                    Handles.DrawWireDisc(farHigh, -direction, targetRadius[s]);
                }
            }
        }

        // 높이 height, 거리 distance의 호를 ±angle 범위로 잘게 나눈 선분들 (DrawLines용 점 쌍)
        private static Vector3[] ArcSegments(float height, float distance, float angle)
        {
            const int steps = 24;
            Vector3[] points = new Vector3[steps * 2];
            for (int i = 0; i < steps; i++)
            {
                points[i * 2] = ArcPoint(height, distance, Mathf.Lerp(-angle, angle, (float)i / steps));
                points[i * 2 + 1] = ArcPoint(height, distance, Mathf.Lerp(-angle, angle, (float)(i + 1) / steps));
            }
            return points;
        }
#endif

        #endregion

        private void StopSpawning()
        {
            if (refilling != null) StopCoroutine(refilling);
            if (waving != null) StopCoroutine(waving);
            refilling = null;
            waving = null;
        }

        // 웨이브가 아닌 표적이 최대 수보다 적으면 refillDelay 뒤 하나를 내보낸다 (맞히거나 작아지기 시작하면 곧바로 채워짐)
        private IEnumerator RefillLoop()
        {
            while (true)
            {
                if (NormalStandingCount() >= targetCount[stage])
                {
                    yield return null;
                    continue;
                }
                yield return new WaitForSeconds(UnityEngine.Random.Range(refillDelay.x, refillDelay.y));
                float jitter = UnityEngine.Random.Range(1f - lifetimeJitter, 1f + lifetimeJitter);
                Spawn(targetLifetime[stage] * jitter, false);
            }
        }

        // waveInterval마다 waveCount개를 한꺼번에 세운다 (서 있던 표적은 그대로, 그 위에 더해짐)
        private IEnumerator WaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(waveInterval);
                Debug.Log($"[PopupPattern] 웨이브 {waveCount[stage]}개");
                for (int i = 0; i < waveCount[stage]; i++) Spawn(waveLifetime, true);
            }
        }

        private void Spawn(float standTime, bool wave)
        {
            Target.Motion motion = PickMotion();
            float radius = targetRadius[stage];
            if (!TryPickCenter(radius, motion, out Vector3 center)) return;

            Target target = IdleTarget();
            fallAt[target] = Time.time + standTime;
            if (wave) waveTargets.Add(target);
            else waveTargets.Remove(target);
            target.Show(center, radius, motion, MaxMoveWidth(center.y, motion));
        }

        // approachChance면 다가오기, 그다음 movingChance면 좌우·위아래·대각선 중 하나, 아니면 제자리
        private Target.Motion PickMotion()
        {
            float roll = UnityEngine.Random.value;
            if (roll < approachChance[stage]) return Target.Motion.Approach;
            if (roll < approachChance[stage] + movingChance[stage]) return moveMotions[UnityEngine.Random.Range(0, moveMotions.Length)];
            return Target.Motion.None;
        }

        // 등장 범위 안 무작위 위치를 positionTries번까지 골라, 겹치지 않고 방금 사라진 곳에서 먼 곳을 찾는다
        private bool TryPickCenter(float radius, Target.Motion motion, out Vector3 center)
        {
            Vector2 distances = motion == Target.Motion.Approach ? approachStart : new Vector2(minDistance, maxDistance[stage]);
            for (int i = 0; i < positionTries; i++)
            {
                float angle = UnityEngine.Random.Range(-spawnAngle[stage], spawnAngle[stage]);
                center = ArcPoint(UnityEngine.Random.Range(heightRange.x, heightRange.y), UnityEngine.Random.Range(distances.x, distances.y), angle);
                if (IsClear(center, radius)) return true;
            }
            center = default;
            return false;
        }

        // 방금 사라진 곳에서 avoidDistance 밖이고, 배경 물체에 묻히거나 머리에서 원판 중심까지 가려지지 않고,
        // 플레이어가 볼 때 서 있거나 작아지는 표적과 겹치지 않음 (두 원판 중심 방향 사이 각도 > 두 원판이 보이는 크기(반지름 ÷ 거리)의 합)
        private bool IsClear(Vector3 center, float radius)
        {
            if (Vector3.Distance(center, lastVacated) < avoidDistance) return false;

            Vector3 eye = viewer.position;
            if (Physics.CheckSphere(center, radius, blockMask, QueryTriggerInteraction.Ignore)) return false;
            if (Physics.Linecast(eye, center, blockMask, QueryTriggerInteraction.Ignore)) return false;

            foreach (Target other in targets)
            {
                if (!other.gameObject.activeSelf) continue;
                Vector3 otherCenter = CenterOf(other);
                float apart = Vector3.Angle(center - eye, otherCenter - eye) * Mathf.Deg2Rad;
                float sizes = radius / Vector3.Distance(center, eye) + RadiusOf(other) / Vector3.Distance(otherCenter, eye);
                if (apart <= sizes) return false;
            }
            return true;
        }

        // 위아래로 오가는 표적이 높이 범위 밖으로 나가지 않는 최대 왕복 폭 (좌우 표적은 제한 없음)
        private float MaxMoveWidth(float height, Target.Motion motion)
        {
            float room = Mathf.Min(height - heightRange.x, heightRange.y - height) * 2f;
            if (motion == Target.Motion.Vertical) return room;
            if (motion == Target.Motion.Diagonal) return room / Mathf.Sin(45f * Mathf.Deg2Rad);
            return float.MaxValue;
        }

        private int NormalStandingCount()
        {
            int count = 0;
            foreach (Target target in targets)
                if (target.IsStanding && !waveTargets.Contains(target)) count++;
            return count;
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

        private void OnTargetHit(Target target, Vector3 point)
        {
            lastVacated = CenterOf(target);
            TargetHit?.Invoke(point);
        }

        // 원점에서 +Z 기준 좌우 angle(°), 수평 거리 distance, 높이 height인 점
        private static Vector3 ArcPoint(float height, float distance, float angle)
        {
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance + Vector3.up * height;
        }

        private static float FlatDistance(Vector3 position)
        {
            position.y = 0f;
            return position.magnitude;
        }

        // 표적 루트는 원판 아래 끝이고 크기 = 지름이다
        private static float RadiusOf(Target target) => target.transform.localScale.x / 2f;
        private static Vector3 CenterOf(Target target) => target.transform.position + Vector3.up * RadiusOf(target);
    }
}
