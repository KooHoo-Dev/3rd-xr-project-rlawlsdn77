using System.Collections;
using TMPro;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 콤보 표시. Score 보드 오른쪽 글자(comboText)에 게임 중 2연속부터 "7 Combo"가 계속 보이고, 오를 때마다 튀어나왔다 줄어든다. 끊기면 사라진다.
    // comboStep의 배수(5, 10, 15…)를 넘으면 그 단계 색·큰 글자가 되고, 그 순간 맞힌 표적 위에 "+n발"이 튀어나왔다 떠오르며,
    // 탄피 소리(단계마다 음이 높아짐)와 빛이 터지는 이펙트가 난다.
    public class UI_Combo : MonoBehaviour
    {
        [Header("게임")]
        [SerializeField] private GameManager gameManager;

        [Header("콤보 표시 (Score 보드 오른쪽)")]
        [SerializeField] private TMP_Text comboText;            // UI/Score/ComboText
        [SerializeField] private float popScale = 1.4f;         // 오를 때 튀어나오는 크기 (배)
        [SerializeField] private float popTime = 0.15f;         // 튀어나왔다 1배로 돌아오는 시간 (초)

        [Header("5의 배수 (보너스)")]
        [SerializeField] private float bonusScale = 1.5f;       // 5연속을 넘은 뒤 콤보 글자 크기 / "+n발" 글자 크기 (배)
        [SerializeField] private Color[] bonusColors =          // 단계별 색 (5, 10, 15, 20…). 넘으면 마지막 색. 애플 시스템 색 (노랑, 주황, 빨강, 보라)
        {
            new Color(1f, 0.839f, 0.039f), new Color(1f, 0.624f, 0.039f), new Color(1f, 0.271f, 0.227f), new Color(0.749f, 0.353f, 0.949f),
        };
        [SerializeField] private AudioClip bonusClip;           // 보너스 소리 (ComboBonus, 비어 있으면 소리 없음)
        [SerializeField] private float pitchStep = 0.1f;        // 단계마다 높아지는 음 (배)

        [Header("+n발 (맞힌 표적 위)")]
        [SerializeField] private TMP_Text popupPrefab;          // ComboPopup.prefab (Ignore Raycast)
        [SerializeField] private float popupLift = 0.5f;        // 맞은 지점에서 이만큼 위에 띄움 (m)
        [SerializeField] private float popupDuration = 0.8f;    // 떠 있는 시간 (초)
        [SerializeField] private float popupRise = 0.3f;        // 그동안 올라가는 높이 (m)
        [SerializeField] private float sizePerMeter = 0.4f;     // 거리 1m당 글자 크기 배율 (멀어도 비슷한 크기로 보이게)
        [SerializeField] private float fadeStart = 0.6f;        // 떠 있는 시간 중 이 비율부터 흐려짐 (0~1)
        [SerializeField] private ParticleSystem burstPrefab;    // 맞힌 자리에 터지는 빛 (ComboBurst.prefab, 끝나면 스스로 사라짐)
        [SerializeField] private float burstSizePerMeter = 0.12f;   // 거리 1m당 이펙트 크기 배율 (멀어도 비슷한 크기로 보이게)
        [SerializeField] private float burstScaleStep = 0.25f;  // 단계마다 커지는 이펙트 크기 (배)

        private int shownCombo;         // comboText에 지금 쓰인 콤보 (바뀔 때만 글자를 다시 씀)
        private float poppedAt = -1f;   // 마지막으로 콤보가 오른 시각 (튀어나오기)
        private Vector3 baseScale;      // comboText 원래 크기

        #region Unity Methods

        private void Awake()
        {
            baseScale = comboText.transform.localScale;
        }

        private void OnEnable()
        {
            gameManager.ComboHit += ShowBonus;
        }

        private void OnDisable()
        {
            gameManager.ComboHit -= ShowBonus;
        }

        // 게임 중 2연속부터 보이고, 오르면 튀어나온다. 끊기거나 게임이 아니면 숨긴다
        private void Update()
        {
            int combo = gameManager.Phase == GameManager.GamePhase.Playing ? gameManager.Combo : 0;
            if (combo != shownCombo)
            {
                if (combo > shownCombo) poppedAt = Time.time;
                shownCombo = combo;
                comboText.gameObject.SetActive(combo >= 2);
                comboText.text = $"<b>{combo}</b> <size=65%>Combo</size>";
                int tier = combo / gameManager.ComboStep;
                comboText.color = tier == 0 ? Color.white : TierColor(tier);
            }
            if (shownCombo < 2) return;

            float styleScale = shownCombo >= gameManager.ComboStep ? bonusScale : 1f;
            float pop = Mathf.Lerp(popScale, 1f, (Time.time - poppedAt) / popTime);
            comboText.transform.localScale = baseScale * (styleScale * pop);
        }

        #endregion

        // comboStep의 배수가 된 순간: 맞힌 표적 위에 "+n발", 소리, 이펙트
        private void ShowBonus(int combo, Vector3 point)
        {
            if (combo % gameManager.ComboStep != 0) return;
            int tier = combo / gameManager.ComboStep;   // 5 → 1, 10 → 2 …
            Color color = TierColor(tier);

            TMP_Text popup = Instantiate(popupPrefab, point + Vector3.up * popupLift, Quaternion.identity);
            popup.text = $"+{gameManager.ComboBonusAmmo}발";
            popup.color = color;
            StartCoroutine(Float(popup, bonusScale));
            PlayBonusSound(1f + pitchStep * (tier - 1));

            if (burstPrefab == null) return;
            ParticleSystem burst = Instantiate(burstPrefab, point, Quaternion.identity);
            float distance = Vector3.Distance(point, Camera.main.transform.position);
            burst.transform.localScale = Vector3.one * (distance * burstSizePerMeter * (1f + burstScaleStep * (tier - 1)));
            foreach (ParticleSystem part in burst.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = part.main;
                main.startColor = color;
            }
        }

        private Color TierColor(int tier) => bonusColors[Mathf.Min(tier, bonusColors.Length) - 1];

        // 튀어나왔다가 떠오르고, 끝 무렵 흐려진 뒤 지운다
        private IEnumerator Float(TMP_Text popup, float scale)
        {
            Transform head = Camera.main.transform;
            Vector3 start = popup.transform.position;
            float size = Vector3.Distance(start, head.position) * sizePerMeter * scale;

            for (float t = 0f; t < popupDuration; t += Time.deltaTime)
            {
                float progress = t / popupDuration;
                float pop = Mathf.Lerp(popScale, 1f, t / popTime);  // popTime 뒤로는 1배
                popup.transform.localScale = Vector3.one * (size * pop);
                popup.transform.position = start + Vector3.up * (popupRise * progress);
                // TextMeshPro 글자는 -Z 쪽에서 바로 읽히므로 +Z가 머리 반대쪽을 보게 한다
                popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - head.position);
                popup.alpha = 1f - Mathf.InverseLerp(fadeStart, 1f, progress);
                yield return null;
            }
            Destroy(popup.gameObject);
        }

        // 2D 소리로 머리에서 들리게 하고, 단계마다 음을 높인다 (PlayClipAtPoint는 음 높이를 못 바꿈)
        private void PlayBonusSound(float pitch)
        {
            if (bonusClip == null) return;
            AudioSource source = new GameObject("ComboBonusSound").AddComponent<AudioSource>();
            source.clip = bonusClip;
            source.pitch = pitch;
            source.Play();
            Destroy(source.gameObject, bonusClip.length / pitch);
        }
    }
}
