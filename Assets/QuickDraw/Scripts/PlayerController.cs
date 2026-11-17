using QuickDraw.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlayerController : MonoBehaviour
{
    // 총은 항상 오른손으로 잡는다
    // 탄창은 항상 왼손으로 잡는다.
    // 발사는 꾹 눌러도 한 발만 발사된다.
    // 컨트롤러를 총 / 탄창에 가져다 대면 스냅 가이드라인이 먼저 보인후 대응되는 입력을 하면 스냅된다.

    // 잡기(그립)는 XRI Near-Far Interactor가 맡는다. 이 스크립트는 오른손에 든 총에 발사 / 탄창 제거만 전달한다.

    // PC 테스트용 입력 매핑 / 바인딩 모음
    // XR_트리거       : 마우스_좌클릭
    // XR_우측 A버튼    : 키보드 R

    // 컨트롤러 입력 액션 모음
    private InputAction ShootAction;    // 트리거      : 발사 / UI 선택 (사격으로 UI 버튼도 클릭 가능)
    private InputAction MagOutAction;   // 오른손 A    : 탄창 제거 (탄을 다 썼을 때만)

    [Header("플레이어의 오른손")]
    [SerializeField] private NearFarInteractor RightHand;   // 총 고정임

    #region Unity Methods

    private void OnEnable()
    {
        ShootAction = new InputAction(
            "Shoot",
            InputActionType.Button,
            "<XRController>{RightHand}/{TriggerButton}"); // 우측 트리거

        ShootAction.AddBinding("<Mouse>/leftButton");   // PC 테스트용 (좌클릭)

        ShootAction.Enable();


        MagOutAction = new InputAction(
            "MagOut",
            InputActionType.Button,
            "<XRController>{RightHand}/{PrimaryButton}"); // 우측 A 키 누름

        MagOutAction.AddBinding("<Keyboard>/r"); // PC 테스트용 (키보드 R 버튼)

        MagOutAction.Enable();
    }

    private void OnDisable()
    {
        ShootAction.Disable();
        ShootAction.Dispose();
        ShootAction = null;

        MagOutAction.Disable();
        MagOutAction.Dispose();
        MagOutAction = null;
    }

    private void Update()
    {
        // 오른손에 총을 들고 있을 때만 입력을 총에 전달한다
        if (RightHand.hasSelection == false) return;
        if (RightHand.firstInteractableSelected.transform.TryGetComponent(out Gun heldGun) == false) return;

        if (ShootAction.WasPressedThisFrame()) heldGun.TryFire();
        if (MagOutAction.WasPressedThisFrame()) heldGun.TryEjectMagazine();   // 탄을 다 쓴 탄창만 빠진다
    }

    #endregion

}
