// 은하: 보라·파랑 성운이 천천히 흐르고 별이 반짝인다. 총 이스터에그(M_GunGalaxy)에 쓴다.
Shader "QuickDraw/Galaxy"
{
    Properties
    {
        _NebulaScale ("Nebula Scale (1/m)", Float) = 30
        _StarDensity ("Star Grid (1/m)", Float) = 300
        _FlowSpeed ("Flow Speed", Float) = 0.15
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

            float _NebulaScale;
            float _StarDensity;
            float _FlowSpeed;

            fixed4 frag(v2f i) : SV_Target
            {
                float3 p = i.objectPos * _NebulaScale;
                float nebula = fbm(p + _Time.y * _FlowSpeed);
                float nebula2 = fbm(p * 1.7 - 5.2);
                float3 color = float3(0.02, 0.01, 0.06)
                             + float3(0.45, 0.1, 0.65) * smoothstep(0.35, 0.8, nebula)
                             + float3(0.05, 0.3, 0.7) * smoothstep(0.45, 0.85, nebula2);

                float3 cell = floor(i.objectPos * _StarDensity);
                float star = step(0.992, hash3(cell)) * (0.6 + 0.4 * sin(_Time.y * 5 + hash3(cell + 3) * 30));

                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float rim = pow(1 - saturate(dot(normalize(i.worldNormal), viewDir)), 3) * 0.35;
                return fixed4(color + star + float3(0.4, 0.3, 1) * rim, 1);
            }
            ENDCG
        }
    }
}
