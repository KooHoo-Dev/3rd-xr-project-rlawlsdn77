using TMPro;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // World Space UI. 단계에 맞는 보드(Menu / Score / Result)만 보인다.
    // 버튼(START / MENU)은 총으로 쏴서 누른다: 버튼 콜라이더는 Shootable 레이어이고, 맞으면 Gun이 이 스크립트의 Shooted를 부른다.
    public class UI : MonoBehaviour, IShootable
    {
        [Header("게임")]
        [SerializeField] private GameManager gameManager;

        [Header("보드")]
        [SerializeField] private GameObject menuBoard;      // 타이틀 + START (Ready)
        [SerializeField] private GameObject scoreBoard;     // 남은 시간(m:ss) / 점수 (Playing)
        [SerializeField] private GameObject resultBoard;    // 점수, 명중률 + MENU (Result)

        [Header("버튼 (쏴서 누름)")]
        [SerializeField] private Collider startButton;
        [SerializeField] private Collider menuButton;

        [Header("글자")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text resultText;

        private int shownSeconds = -1;      // Score 보드에 지금 쓰인 남은 시간 (바뀔 때만 글자를 다시 만듦)
        private int shownScore = -1;

        public void Shooted(RaycastHit hit)
        {
            if (hit.collider == startButton) gameManager.StartGame();
            else if (hit.collider == menuButton) gameManager.ReturnToMenu();
        }

        #region Unity Methods

        private void OnEnable()
        {
            gameManager.PhaseChanged += ShowPhase;
            ShowPhase();
        }

        private void OnDisable()
        {
            gameManager.PhaseChanged -= ShowPhase;
        }

        private void Update()
        {
            if (!scoreBoard.activeSelf) return;
            int seconds = Mathf.CeilToInt(gameManager.RemainingTime);
            if (seconds == shownSeconds && gameManager.Score == shownScore) return;
            shownSeconds = seconds;
            shownScore = gameManager.Score;
            scoreText.text = $"{seconds / 60}:{seconds % 60:00}   점수 {shownScore}";
        }

        #endregion

        private void ShowPhase()
        {
            GameManager.GamePhase phase = gameManager.Phase;
            shownSeconds = -1;  // 새 판이 시작되면 글자를 다시 쓴다
            menuBoard.SetActive(phase == GameManager.GamePhase.Ready);
            scoreBoard.SetActive(phase == GameManager.GamePhase.Playing);
            resultBoard.SetActive(phase == GameManager.GamePhase.Result);

            if (phase == GameManager.GamePhase.Result)
                resultText.text = $"점수 {gameManager.Score}\n명중률 {Mathf.RoundToInt(gameManager.Accuracy * 100f)}% ({gameManager.HitCount}/{gameManager.ShotCount})";
        }
    }
}
