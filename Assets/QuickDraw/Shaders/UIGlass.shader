// UI 보드·버튼용 둥근 사각형 (애플 visionOS 느낌의 반투명 글래스 / 알약 모양 버튼).
// Quad(1×1)에 붙이고 Transform 스케일로 크기(m)를 정한다. 모서리 반지름(_Radius, m)은 짧은 변의 절반까지만 커지므로
// 아주 크게 두면 알약(가로가 길 때)이나 원(정사각형일 때)이 된다.
// 글자(TextMeshPro, Transparent 3000)보다 먼저 그리도록 큐를 Transparent-10으로 둔다 (버튼 머티리얼은 2995로 판 위에 그림).
// _Highlight(0~1)는 UI 스크립트가 MaterialPropertyBlock으로 넣는 겨눔 강조.
Shader "QuickDraw/UIGlass"
{
    Properties
    {
        _ColorTop ("Color Top", Color) = (0.2, 0.2, 0.22, 0.8)
        _ColorBottom ("Color Bottom", Color) = (0.12, 0.12, 0.14, 0.84)
        _RimColor ("Rim Color", Color) = (1, 1, 1, 0.18)
        _RimWidth ("Rim Width (m)", Float) = 0.008
        _Radius ("Corner Radius (m)", Float) = 0.12
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 0.22)
        _Highlight ("Highlight", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        // 깊이를 써서 뒤에 있는 반투명 물체(창유리 등, 큐 3000)가 나중에 판 위로 그려지지 않게 한다. 모양 밖은 clip으로 버림
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _ColorTop, _ColorBottom, _RimColor, _HighlightColor;
            float _RimWidth, _Radius, _Highlight;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 local : TEXCOORD1;      // 중심에서의 위치 (m)
                float2 halfSize : TEXCOORD2;   // 반 크기 (m)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float2 size = float2(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21));
                o.local = v.vertex.xy * size;
                o.halfSize = size * 0.5;
                return o;
            }

            // 둥근 사각형까지의 부호 있는 거리 (안쪽이 음수)
            float roundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float radius = min(_Radius, min(i.halfSize.x, i.halfSize.y));
                float d = roundedBox(i.local, i.halfSize, radius);
                float aa = max(fwidth(d), 1e-5);
                float shape = saturate(0.5 - d / aa);
                clip(shape - 0.01);

                fixed4 color = lerp(_ColorBottom, _ColorTop, i.uv.y);
                color.rgb = lerp(color.rgb, _HighlightColor.rgb, _HighlightColor.a * _Highlight);

                // 가장자리 안쪽 얇은 빛 테두리, 위쪽일수록 밝게 (빛이 위에서 오는 느낌)
                float rim = saturate(1 - abs(d + _RimWidth * 0.5) / (_RimWidth * 0.5 + aa));
                float rimAlpha = rim * _RimColor.a * lerp(0.35, 1, i.uv.y);
                color.rgb = lerp(color.rgb, _RimColor.rgb, rimAlpha);
                color.a = saturate(color.a + rimAlpha) * shape;
                return color;
            }
            ENDCG
        }
    }
}
