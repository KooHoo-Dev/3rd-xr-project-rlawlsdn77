// 용암: 검은 바위에 빛나는 주황 균열(보로노이 경계)이 맥동한다. 총 이스터에그(M_GunLava)에 쓴다.
Shader "QuickDraw/Lava"
{
    Properties
    {
        _CellScale ("Cell Scale (1/m)", Float) = 45
        _CrackWidth ("Crack Width", Float) = 0.12
        _PulseSpeed ("Pulse Speed", Float) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "GunSkin.cginc"

            float _CellScale;
            float _CrackWidth;
            float _PulseSpeed;

            fixed4 frag(v2f i) : SV_Target
            {
                // 가장 가까운 두 점까지 거리 차가 작으면 균열 (셀 경계)
                float3 p = i.objectPos * _CellScale;
                float3 cell = floor(p);
                float nearest = 9, second = 9;
                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    float3 c = cell + float3(x, y, z);
                    float3 seed = c + float3(hash3(c), hash3(c + 7.1), hash3(c + 13.7));
                    float d = length(p - seed);
                    if (d < nearest) { second = nearest; nearest = d; }
                    else if (d < second) second = d;
                }

                float crack = 1 - smoothstep(0.0, _CrackWidth, second - nearest);
                float pulse = 0.75 + 0.25 * sin(_Time.y * _PulseSpeed + fbm(p) * 6);
                float3 rock = shade(float3(0.07, 0.06, 0.06), normalize(i.worldNormal), i.worldPos, 30, 0.15);
                float3 glow = lerp(float3(1, 0.25, 0.02), float3(1, 0.85, 0.3), crack * pulse);
                return fixed4(lerp(rock, glow * pulse, crack), 1);
            }
            ENDCG
        }
    }
}
