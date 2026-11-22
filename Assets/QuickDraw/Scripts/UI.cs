using TMPro;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // World Space UI. 단계에 맞는 보드(Menu + 튜토리얼 / Countdown / Score / Result)만 보인다.
    // 버튼(START / MENU)은 총으로 쏴서 누른다: 버튼 콜라이더는 Shootable 레이어이고, 맞으면 Gun이 이 스크립트의 Shooted를 부른다.
    // 튜토리얼 보드도 쏠 수 있다: hitsPerSkin발마다 총 겉모습이 바뀌는 이스터에그.
    public class UI : MonoBehaviour, IShootable
    {
        [Header("게임")]
        [SerializeField] private GameManager gameManager;

        [Header("보드")]
        [SerializeField] private GameObject menuBoard;      // 타이틀 + START (Ready)
        [SerializeField] private GameObject countdownBoard; // 큰 숫자 3·2·1 (Countdown)
        [SerializeField] private GameObject scoreBoard;     // 남은 시간(m:ss) / 점수 (Playing)
        [SerializeField] private GameObject resultBoard;    // 점수, 명중률 + MENU (Result)

        [Header("버튼 (쏴서 누름)")]
        [SerializeField] private Collider startButton;
        [SerializeField] private Collider menuButton;

        [Header("글자")]
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text resultText;

        [Header("카운트다운 소리 (머리 위치에서 재생, 비어 있으면 소리 없음)")]
        [SerializeField] private AudioClip countdownClip;   // 숫자가 바뀔 때 (CountdownTick)
        [SerializeField] private AudioClip goClip;          // 게임이 시작될 때 (CountdownGo)

        [Header("튜토리얼")]
        [SerializeField] private GameObject tutorialBoard;  // 조작 안내 (Ready)

        [Header("튜토리얼 이스터에그")]
        [SerializeField] private Collider tutorialCollider;     // 튜토리얼 보드 (Shootable)
        [SerializeField] private Gun gun;
        [SerializeField] private Material[] gunSkins;           // 차례로 바뀌는 총 겉모습 (황금, 은하, 용암, 진주광). 다 돌면 원래 모습
        [SerializeField] private int hitsPerSkin = 10;          // 이만큼 맞힐 때마다 다음 겉모습

        [Header("움직임")]
        [SerializeField] private float countdownPopScale = 1.25f;   // 카운트다운 숫자가 바뀔 때 커졌다 돌아오는 크기 (배)
        [SerializeField] private float countdownPopTime = 0.25f;    // 1배로 돌아오는 시간 (초)
        [SerializeField] private float hoverSpeed = 12f;            // 총으로 버튼을 겨눌 때 밝아지고 돌아오는 빠르기

        private static readonly int HighlightId = Shader.PropertyToID("_Highlight");

        private int shownSeconds = -1;      // Countdown / Score 보드에 지금 쓰인 남은 시간 (바뀔 때만 글자를 다시 만듦)
        private int shownScore = -1;
        private int tutorialHits;           // 튜토리얼 보드를 맞힌 수 (게임을 켜 둔 동안 이어짐)
        private float countdownPoppedAt = -1f;
        private Vector3 countdownBaseScale;
        private Collider[] buttons;
        private float[] buttonHighlights;
        private MaterialPropertyBlock highlightBlock;

        public void Shooted(RaycastHit hit)
        {
            if (hit.collider == startButton) gameManager.StartGame();
            if (hit.collider == menuButton) gameManager.ReturnToMenu();
            if (hit.collider == tutorialCollider) CountTutorialHit();
        }

        #region Unity Methods

        private void Awake()
        {
            countdownBaseScale = countdownText.transform.localScale;
            buttons = new[] { startButton, menuButton };
            buttonHighlights = new float[buttons.Length];
            highlightBlock = new MaterialPropertyBlock();
        }

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
            if (countdownBoard.activeSelf) ShowCountdown();
            if (scoreBoard.activeSelf) ShowScore();
            HighlightAimedButton();
        }

        #endregion

        private void ShowPhase()
        {
            GameManager.GamePhase phase = gameManager.Phase;
            shownSeconds = -1;  // 새 판이 시작되면 글자를 다시 쓴다
            menuBoard.SetActive(phase == GameManager.GamePhase.Ready);
            tutorialBoard.SetActive(phase == GameManager.GamePhase.Ready);
            countdownBoard.SetActive(phase == GameManager.GamePhase.Countdown);
            scoreBoard.SetActive(phase == GameManager.GamePhase.Playing);
            resultBoard.SetActive(phase == GameManager.GamePhase.Result);

            if (phase == GameManager.GamePhase.Playing) PlayAtHead(goClip);
            // 큰 점수 숫자 위에 회색 작은 이름표, 아래에 명중률 (애플 스타일: 숫자는 굵고 크게, 설명은 작고 흐리게)
            if (phase == GameManager.GamePhase.Result)
                resultText.text = "<size=50%><alpha=#8C>점수</size>\n<alpha=#FF><b>" + gameManager.Score + "</b>\n"
                    + $"<size=50%><alpha=#8C>명중률  <alpha=#FF><b>{Mathf.RoundToInt(gameManager.Accuracy * 100f)}%</b>"
                    + $"<alpha=#8C>  ·  {gameManager.HitCount}/{gameManager.ShotCount}</size>";
        }

        // 남은 카운트다운을 올림한 숫자(3 → 2 → 1)를 보이고, 숫자가 바뀔 때마다 소리를 내며 살짝 커졌다 돌아온다
        private void ShowCountdown()
        {
            float pop = Mathf.SmoothStep(countdownPopScale, 1f, (Time.time - countdownPoppedAt) / countdownPopTime);
            countdownText.transform.localScale = countdownBaseScale * pop;

            int seconds = Mathf.CeilToInt(gameManager.RemainingTime);
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            countdownPoppedAt = Time.time;
            countdownText.text = seconds.ToString();
            PlayAtHead(countdownClip);
        }

        // 남은 시간은 고정폭 숫자(바뀌어도 글자가 흔들리지 않게), 점수는 회색 이름표 + 굵은 숫자
        private void ShowScore()
        {
            int seconds = Mathf.CeilToInt(gameManager.RemainingTime);
            if (seconds == shownSeconds && gameManager.Score == shownScore) return;
            shownSeconds = seconds;
            shownScore = gameManager.Score;
            scoreText.text = $"<b><mspace=0.58em>{seconds / 60}:{seconds % 60:00}</mspace></b>"
                + $"<alpha=#40>   |   <alpha=#8C><size=70%>점수</size>  <alpha=#FF><b>{shownScore}</b>";
        }

        // 총이 겨누는 버튼(START / MENU)을 부드럽게 밝힌다 (visionOS의 시선 강조처럼)
        private void HighlightAimedButton()
        {
            GameObject aimed = gun != null ? gun.AimTarget : null;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null || !buttons[i].gameObject.activeInHierarchy) continue;
                float target = aimed == buttons[i].gameObject ? 1f : 0f;
                if (Mathf.Approximately(buttonHighlights[i], target)) continue;
                buttonHighlights[i] = Mathf.MoveTowards(buttonHighlights[i], target, hoverSpeed * Time.deltaTime);
                Renderer button = buttons[i].GetComponent<Renderer>();
                button.GetPropertyBlock(highlightBlock);
                highlightBlock.SetFloat(HighlightId, buttonHighlights[i]);
                button.SetPropertyBlock(highlightBlock);
            }
        }

        // 보드는 4.1m 앞이라 그 자리에서 내면 작게 들리므로 플레이어 머리(메인 카메라) 위치에서 낸다
        private void PlayAtHead(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }

        // 튜토리얼 보드를 hitsPerSkin발 맞힐 때마다 총 겉모습이 gunSkins 순서대로 바뀌고, 다 돌면 원래 모습으로 돌아간다
        private void CountTutorialHit()
        {
            tutorialHits++;
            if (tutorialHits % hitsPerSkin != 0) return;
            int skin = tutorialHits / hitsPerSkin % (gunSkins.Length + 1);     // 0 = 원래 모습
            gun.SetSkin(skin == 0 ? null : gunSkins[skin - 1]);
            Debug.Log($"[UI] 이스터에그: 총 겉모습 {(skin == 0 ? "원래" : gunSkins[skin - 1].name)} (튜토리얼 {tutorialHits}발)");
        }
    }
}
