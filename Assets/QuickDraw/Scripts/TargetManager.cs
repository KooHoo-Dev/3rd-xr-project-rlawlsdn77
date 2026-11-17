using UnityEngine;

namespace QuickDraw.Scripts
{
    public class TargetManager : MonoBehaviour
    {
        // 타겟의 스폰 및 이동을 담당하는 매니저
        // 타겟은 최소 / 최대 개수가 정해져 있으며
        // 타겟은 직사각형 내의 스폰 영역에서 등장하며,
        // 이동은 각 직사각형의 가장자리와 모서리 중앙 (8방향)에서 스폰되어서 반대 방향으로 이동한다.
        // 타겟 군집은 미리 구워둔 Prefab 형태의 군집이 무작위로 등장하여 이동한다.
        
        [Header("타겟 수")]
        [SerializeField] private int MaxTarget;
        [SerializeField] private int MinTarget;

        [Header("타겟 등장 방향벡터")] 
        [SerializeField] private Vector3 moveTarget;
        
        
        public Target target;
        
        private void Update()
        {
            target = new Target(); // 새 인스턴스 생성
        }
    }
}