using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace QuickDraw.Scripts
{
    // PC 테스트용 XR 시뮬레이터에서 제자리를 지킨다. XR Origin (XR Rig)에 붙인다.
    // (헤드셋에서는 씬에서 Locomotion·Teleport를 꺼 두어 이미 움직이지 않는다)
    // - FPS 모드(헤드셋과 양손이 함께 움직임)일 때만 WASD 이동을 막는다. 이동 배율은 컨트롤러 하나만 움직일 때도 쓰여서 항상 0으로 둘 수 없다.
    // - Point-and-Click을 끈다. 커서 레이캐스트가 손에 든 총에 맞으면 컨트롤러가 튄다.
    public class PlayerMoveLock : MonoBehaviour
    {
        private XRInteractionSimulator simulator;
        private float originalMultiplier;       // 시뮬레이터 원래 이동 배율
        private bool originalPointAndClick;     // 시뮬레이터 원래 Point-and-Click 사용 여부

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
