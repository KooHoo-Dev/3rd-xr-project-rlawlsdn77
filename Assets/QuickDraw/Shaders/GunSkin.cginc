// 총 이스터에그 셰이더(Galaxy, Lava, Pearl)가 같이 쓰는 코드.
// 무늬는 총의 오브젝트 좌표(m)로 계산해서 총이 움직여도 무늬가 따라간다.
// VR 양눈 렌더링(single-pass instanced)을 위해 인스턴싱·스테레오 매크로를 넣었다.
#ifndef GUN_SKIN_INCLUDED
#define GUN_SKIN_INCLUDED

#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 pos : SV_POSITION;
    float3 objectPos : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert(appdata v)
{
    v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.objectPos = v.vertex.xyz;
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.worldNormal = UnityObjectToWorldNormal(v.normal);
    return o;
}

float hash3(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// 0~1 값 노이즈
float valueNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(lerp(hash3(i), hash3(i + float3(1, 0, 0)), f.x),
                     lerp(hash3(i + float3(0, 1, 0)), hash3(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(hash3(i + float3(0, 0, 1)), hash3(i + float3(1, 0, 1)), f.x),
                     lerp(hash3(i + float3(0, 1, 1)), hash3(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

float fbm(float3 p)
{
    return valueNoise(p) * 0.5 + valueNoise(p * 2.03) * 0.3 + valueNoise(p * 4.01) * 0.2;
}

// 고정된 빛 방향으로 확산광 + 하이라이트 (씬 조명 없이도 모양이 보이게)
float3 shade(float3 albedo, float3 normal, float3 worldPos, float specPower, float specAmount)
{
    float3 lightDir = normalize(float3(-0.4, 0.8, -0.5));
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    float3 halfDir = normalize(lightDir + viewDir);
    float diffuse = saturate(dot(normal, lightDir));
    float specular = pow(saturate(dot(normal, halfDir)), specPower) * specAmount;
    return albedo * (0.35 + 0.75 * diffuse) + specular;
}

#endif
