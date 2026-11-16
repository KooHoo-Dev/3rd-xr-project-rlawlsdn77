using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PlayerController : MonoBehaviour
{
    // 총은 항상 오른손으로 잡는다
    // 탄창은 항상 왼손으로 잡는다.
    // 발사는 꾹 눌러도 한 발만 발사된다.
    // 컨트롤러를 총 / 탄창에 가져다 대면 스냅 가이드라인이 먼저 보인후 대응되는 입력을 하면 스냅된다.
    
    // PC 테스트용 입력 매핑 / 바인딩 모음
    // XR_그립         : 마우스_우클릭
    // XR_트리거       : 마우스_좌클릭
    // XR_우측 A버튼    : 키보드 R
    
    // 컨트롤러 입력 액션 모음
    private InputAction TakeAction;     // 그립        : 총, 탄창 잡기 (컨트롤러에 스냅된다)
    private InputAction ShootAction;    // 트리거      : 발사 / UI 선택 (사격으로 UI 버튼도 클릭 가능)
    private InputAction MagOutAction;   // A 또는 X    : 탄창 제거 

    [Header("플레이어의 왼손 / 오른손")] 
    [SerializeField] private GameObject RightHand;   // 총 고정임
    [SerializeField] private GameObject LeftHand;    // 탄창 고정임
    
    // 플레이어가 조준하는 위치에 상호작용되는 오브젝트 (표적 / UI 포함)
    public GameObject InteractObj;
    
    #region Input Actions

    private void Take()
    {
        
    }

    private void Shoot()
    {
        
    }

    private void MagOut()
    {
        
    }

    #endregion
    
    #region Unity Methods

    private void Awake()
    {
        // XR 인풋 매니저 참조
        RightHand = GetComponent<XRInputModalityManager>().rightHand;
        LeftHand = GetComponent<XRInputModalityManager>().leftHand;
    }

    private void OnEnable()
    {
        TakeAction = new InputAction(
            "Take",
            InputActionType.Button,
            "<XRController>{RightHand}/{GripButton}"); // 우측 그립

        TakeAction.AddBinding("<Mouse>/rightButton");     // PC 테스트용 (우클릭)
        
        TakeAction.Enable();
        
        
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
        TakeAction.Disable();
        TakeAction.Dispose();
        TakeAction = null;
        
        ShootAction.Disable();
        ShootAction.Dispose();
        ShootAction = null;
        
        MagOutAction.Disable();
        MagOutAction.Dispose();
        MagOutAction = null;
    }
    
    private void Update()
    {
        // PC 환경 테스트 코드
        if (TakeAction.WasPressedThisFrame())
        {
            Debug.Log("잡기 버튼을 입력받았습니다.");
        }

        if (ShootAction.WasPressedThisFrame())
        {
            Debug.Log("발사 버튼을 입력받았습니다.");
        }

        if (MagOutAction.WasPressedThisFrame())
        {
            Debug.Log("탄창 제거 버튼을 입력받았습니다.");
        }
    }

    #endregion
    
}
