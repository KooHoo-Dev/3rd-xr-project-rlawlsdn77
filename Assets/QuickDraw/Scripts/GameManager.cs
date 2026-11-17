using System;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 한 판의 흐름: Ready(메뉴) → START → Playing(gameDuration, 쉬는 시간 없음) → Result → MENU → Ready
    // 게임 시간을 단계 수(targetRadius 길이)로 똑같이 나눠, 단계마다 표적이 작아지고·많아지고·빨리 쓰러진다 (기본 30초씩 3단계).
    // 단계가 바뀌어도 서 있던 표적은 그대로 두고, 새로 나오는 표적부터 다음 단계 수치를 쓴다. 게임이 끝나면 남은 표적이 한꺼번에 쓰러진다.
    // 쏜 수와 맞힌 수는 Playing 단계에서만 센다 (START / MENU를 쏜 탄은 명중률에 안 들어감). 메뉴로 돌아갈 때 총을 비운다.
    public class GameManager : MonoBehaviour
    {
        public enum GamePhase { Ready, Playing, Result }

        [Header("게임 시간")]
        [SerializeField] private float gameDuration = 90f;                          // 한 판 시간 (초)

        [Header("단계별 표적 (길이 = 단계 수, 게임 시간을 똑같이 나눔)")]
        [SerializeField] private float[] targetRadius = { 0.5f, 0.35f, 0.25f };     // 표적 반지름 (m)
        [SerializeField] private int[] targetCount = { 3, 4, 5 };                   // 동시에 서 있는 표적 수
        [SerializeField] private float[] targetLifetime = { 1.5f, 1.2f, 1f };       // 표적이 서 있는 시간 (초)

        [Header("점수")]
        [SerializeField] private int scorePerHit = 10;                              // 표적 하나 맞힐 때 점수

        [Header("연결")]
        [SerializeField] private Gun gun;
        [SerializeField] private PopupPattern popup;

        public GamePhase Phase { get; private set; }
        public float RemainingTime { get; private set; }        // 게임의 남은 시간 (초)
        public int Score { get; private set; }
        public int ShotCount { get; private set; }
        public int HitCount { get; private set; }
        public float Accuracy => ShotCount == 0 ? 0f : (float)HitCount / ShotCount;
        public event Action PhaseChanged;                       // UI가 보드를 바꿈

        private int stage;      // 지금 표적 수치 단계 (0부터)

        // UI의 START 표적을 맞히면 부른다
        public void StartGame()
        {
            if (Phase != GamePhase.Ready) return;
            Score = 0;
            ShotCount = 0;
            HitCount = 0;
            RemainingTime = gameDuration;
            stage = -1;
            SetPhase(GamePhase.Playing);
            UpdateStage();
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
            if (Phase != GamePhase.Playing) return;
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

        #endregion

        private void EnterReady()
        {
            gun.EjectMagazine();    // 매 판 빈 총으로 시작 (탄창이 없으면 아무 일 없음)
            SetPhase(GamePhase.Ready);
        }

        // 지난 시간에 맞는 단계로 바뀌었으면 그 단계 수치로 표적을 내보낸다 (서 있던 표적은 그대로)
        private void UpdateStage()
        {
            int stages = targetRadius.Length;
            int current = Mathf.Min((int)((gameDuration - RemainingTime) / (gameDuration / stages)), stages - 1);
            if (current == stage) return;
            stage = current;
            popup.Begin(targetRadius[stage], targetCount[stage], targetLifetime[stage]);
            Debug.Log($"[GameManager] {stage + 1}단계 (남은 시간 {RemainingTime:0}초)");
        }

        private void SetPhase(GamePhase phase)
        {
            Phase = phase;
            Debug.Log($"[GameManager] {phase} (점수 {Score}, {HitCount}/{ShotCount})");
            PhaseChanged?.Invoke();
        }

        private void OnFired()
        {
            if (Phase == GamePhase.Playing) ShotCount++;
        }

        private void OnTargetHit()
        {
            if (Phase != GamePhase.Playing) return;
            HitCount++;
            Score += scorePerHit;
        }
    }
}
