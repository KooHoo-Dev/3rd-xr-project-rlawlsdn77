using System;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 한 판의 흐름: Ready(메뉴) → START → Countdown(3·2·1) → Playing(gameDuration, 쉬는 시간 없음) → Result → MENU → Ready
    // 게임 시간을 PopupPattern의 단계 수로 똑같이 나눠 단계마다 popup.Begin(단계)을 부른다 (기본 30초씩 3단계). 단계별 표적 수치는 PopupPattern이 가진다.
    // 단계가 바뀌어도 서 있던 표적은 그대로 두고, 새로 나오는 표적부터 다음 단계 수치를 쓴다. 게임이 끝나면 남은 표적이 한꺼번에 쓰러진다.
    // 쏜 수·맞힌 수·콤보는 Playing 단계에서만 센다 (START / MENU / 카운트다운 중 쏜 탄은 안 셈). 메뉴로 돌아갈 때 총을 비운다.
    // 콤보: 표적을 맞히면 +1, 표적에 맞지 않은 탄을 쏘면 0. comboStep의 배수마다 총의 탄창이 comboBonusAmmo발 늘어난다 (그 판 끝까지).
    public class GameManager : MonoBehaviour
    {
        public enum GamePhase { Ready, Countdown, Playing, Result }

        [Header("게임 시간")]
        [SerializeField] private float countdownDuration = 3f;                      // START 뒤 카운트다운 (초)
        [SerializeField] private float gameDuration = 90f;                          // 한 판 시간 (초)

        [Header("점수")]
        [SerializeField] private int scorePerHit = 10;                              // 표적 하나 맞힐 때 점수

        [Header("콤보")]
        [SerializeField] private int comboStep = 5;                                 // 이만큼 연속으로 맞힐 때마다 보너스
        [SerializeField] private int comboBonusAmmo = 5;                            // 보너스로 늘어나는 탄 수 (지금 탄창 + 앞으로 끼울 탄창)

        [Header("연결")]
        [SerializeField] private Gun gun;
        [SerializeField] private PopupPattern popup;

        public GamePhase Phase { get; private set; }
        public float RemainingTime { get; private set; }        // 이 단계의 남은 시간 (초, Countdown: 카운트다운, Playing: 게임)
        public int Score { get; private set; }
        public int ShotCount { get; private set; }
        public int HitCount { get; private set; }
        public int Combo { get; private set; }                  // 지금 연속으로 맞힌 수
        public float Accuracy => ShotCount == 0 ? 0f : (float)HitCount / ShotCount;
        public int ComboStep => comboStep;
        public int ComboBonusAmmo => comboBonusAmmo;
        public event Action PhaseChanged;                       // UI가 보드를 바꿈
        public event Action<int, Vector3> ComboHit;             // 표적을 맞혀 콤보가 올랐음 (콤보, 맞은 지점. UI_Combo가 숫자를 띄움)

        private int stage;              // 지금 표적 단계 (0부터)
        private bool shotPending;       // 쏜 탄이 표적에 맞았는지 아직 모름 (TargetHit가 오면 지움)

        // UI의 START 표적을 맞히면 부른다
        public void StartGame()
        {
            if (Phase != GamePhase.Ready) return;
            Score = 0;
            ShotCount = 0;
            HitCount = 0;
            Combo = 0;
            RemainingTime = countdownDuration;
            SetPhase(GamePhase.Countdown);
        }

        // 결과 화면의 MENU 표적을 맞히면 부른다. 총에 꽂힌 탄창은 남은 탄과 함께 빠진다
        public void ReturnToMenu()
        {
            if (Phase != GamePhase.Result) return;
            EnterReady();
        }

        #region Unity Methods

        private void Start()
        {
            gun.Fired += OnFired;
            popup.TargetHit += OnTargetHit;
            EnterReady();
        }

        private void Update()
        {
            if (Phase == GamePhase.Countdown) UpdateCountdown();
            else if (Phase == GamePhase.Playing) UpdatePlaying();
        }

        #endregion

        private void EnterReady()
        {
            gun.EjectMagazine();    // 매 판 빈 총으로 시작 (탄창이 없으면 아무 일 없음)
            gun.ResetCapacity();    // 콤보 보너스는 그 판까지. 메뉴에서 끼우는 탄창부터 다시 기본 발수
            SetPhase(GamePhase.Ready);
        }

        // 카운트다운이 끝나면 게임을 시작한다
        private void UpdateCountdown()
        {
            RemainingTime -= Time.deltaTime;
            if (RemainingTime > 0f) return;

            RemainingTime = gameDuration;
            stage = -1;
            SetPhase(GamePhase.Playing);
            UpdateStage();
        }

        private void UpdatePlaying()
        {
            // 지난 프레임에 쏜 탄이 표적에 맞지 않았으면 빗나감 (Fired와 TargetHit는 TryFire 한 번 안에서 차례로 옴)
            if (shotPending)
            {
                shotPending = false;
                Combo = 0;
            }

            RemainingTime -= Time.deltaTime;
            if (RemainingTime > 0f)
            {
                UpdateStage();
                return;
            }

            RemainingTime = 0f;
            popup.End();
            SetPhase(GamePhase.Result);
        }

        // 지난 시간에 맞는 단계로 바뀌었으면 그 단계로 표적을 내보낸다 (서 있던 표적은 그대로)
        private void UpdateStage()
        {
            int stages = popup.StageCount;
            int current = Mathf.Min((int)((gameDuration - RemainingTime) / (gameDuration / stages)), stages - 1);
            if (current == stage) return;
            stage = current;
            popup.Begin(stage);
            Debug.Log($"[GameManager] {stage + 1}단계 (남은 시간 {RemainingTime:0}초)");
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            shotPending = false;
            Debug.Log($"[GameManager] {phase} (점수 {Score}, {HitCount}/{ShotCount})");
            PhaseChanged?.Invoke();
        }

        private void OnFired()
        {
            if (Phase != GamePhase.Playing) return;
            ShotCount++;
            shotPending = true;
        }

        private void OnTargetHit(Vector3 point)
        {
            if (Phase != GamePhase.Playing) return;
            shotPending = false;
            HitCount++;
            Score += scorePerHit;
            Combo++;
            if (Combo % comboStep == 0) gun.AddCapacity(comboBonusAmmo);
            ComboHit?.Invoke(Combo, point);
        }
    }
}
