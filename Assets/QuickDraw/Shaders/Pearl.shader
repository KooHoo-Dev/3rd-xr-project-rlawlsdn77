// 진주광: 흰 바탕에 보는 각도에 따라 파스텔 무지갯빛이 바뀐다. 총 이스터에그(M_GunPearl)에 쓴다.
Shader "QuickDraw/Pearl"
{
    Properties
    {
        _Iridescence ("Iridescence", Range(0, 1)) = 0.55
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

            float _Iridescence;

            float3 hue(float h)
            {
                return saturate(abs(frac(h + float3(0, 2.0 / 3, 1.0 / 3)) * 6 - 3) - 1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float facing = saturate(dot(normal, viewDir));
                float3 rainbow = hue(facing * 1.6 + i.objectPos.x * 4 + 0.55);
                float3 color = lerp(float3(0.92, 0.9, 0.95), rainbow, _Iridescence * (1 - facing * 0.6));
                return fixed4(shade(color, normal, i.worldPos, 80, 0.7), 1);
            }
            ENDCG
        }
    }
}
