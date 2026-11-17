using TMPro;
using UnityEngine;

namespace QuickDraw.Scripts
{
    // 총 뒤쪽에 남은 탄 수를 숫자로 보여준다. Gun과 같은 오브젝트에 붙이고, 총의 자식 TextMeshPro(AmmoText)를 연결한다.
    [RequireComponent(typeof(Gun))]
    public class UI_Gun : MonoBehaviour
    {
        [Header("남은 탄 표시")]
        [SerializeField] private TMP_Text ammoText;     // 총 뒤쪽 숫자

        private Gun gun;

        #region Unity Methods

        private void Awake()
        {
            gun = GetComponent<Gun>();
        }

        private void OnEnable()
        {
            gun.AmmoChanged += ShowAmmo;
            ShowAmmo(gun.Ammo);
        }

        private void OnDisable()
        {
            gun.AmmoChanged -= ShowAmmo;
        }

        #endregion

        private void ShowAmmo(int ammo)
        {
            ammoText.text = ammo.ToString();
        }
    }
}
