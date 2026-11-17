using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace QuickDraw.Scripts
{
    // 제자리 게임이라 이동을 막는다. XR Origin (XR Rig)에 붙인다.
    // XRI Locomotion을 끄고, PC 테스트용 XR 시뮬레이터의 WASD 이동(시점 회전과 컨트롤러 개별 조작은 유지)을 막는다.
    // 시뮬레이터의 Point-and-Click도 끈다. 커서 레이캐스트가 손에 든 총에 맞으면 컨트롤러가 튄다.
    public class PlayerMoveLock : MonoBehaviour
    {
        private XRInteractionSimulator simulator;
        private float originalMultiplier;       // 시뮬레이터 원래 이동 배율
        private bool originalPointAndClick;     // 시뮬레이터 원래 Point-and-Click 사용 여부

        private void Start()
        {
            foreach (LocomotionProvider provider in GetComponentsInChildren<LocomotionProvider>(true))
                provider.enabled = false;
        }

        private void Update()
        {
            // 시뮬레이터는 씬 로드 뒤에 생기므로 매 프레임 확인한다
            XRInteractionSimulator current = XRInteractionSimulator.instance;
            if (current == null) return;

            if (current != simulator)
            {
                simulator = current;
                originalMultiplier = current.bodyTranslateMultiplier;
                originalPointAndClick = current.usePointAndClick;
                simulator.usePointAndClick = false;
            }

            // FPS 모드(헤드셋과 양손이 함께 움직임)일 때만 이동 배율을 0으로
            simulator.bodyTranslateMultiplier = simulator.manipulatingFPS ? 0f : originalMultiplier;
        }

        // 시뮬레이터는 씬이 바뀌어도 남으므로 원래 값으로 되돌린다
        private void OnDisable()
        {
            if (simulator != null)
            {
                simulator.bodyTranslateMultiplier = originalMultiplier;
                simulator.usePointAndClick = originalPointAndClick;
            }
            simulator = null;
        }
    }
}
