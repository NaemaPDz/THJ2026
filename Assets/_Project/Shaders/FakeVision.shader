
Shader "ThaiHorrorJam/FakeVision"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _OcclusionTex ("Occlusion Mask", 2D) = "white" {}
        _SelectiveTex ("Selective Objects", 2D) = "black" {}
        _UnoccludedMonsterTex ("Unoccluded Monsters", 2D) = "black" {}

        _Darkness ("Darkness", Range(0, 1)) = 0.94

        _NearRadius ("Near Vision Radius", Float) = 1.8
        _NearSoftness ("Near Edge Softness", Float) = 0.45

        _ConeLength ("Cone Length", Float) = 6.0
        _ConeAngle ("Cone Angle", Range(10, 160)) = 70
        _ConeSoftness ("Cone Edge Softness", Float) = 0.35
        _ConeAngleSoftness ("Cone Angle Softness", Float) = 5
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OcclusionTex;
            sampler2D _SelectiveTex;
            sampler2D _UnoccludedMonsterTex;

            float4 _MainTex_TexelSize;

            float _Darkness;
            float _NearRadius;
            float _NearSoftness;

            float _ConeLength;
            float _ConeAngle;
            float _ConeSoftness;
            float _ConeAngleSoftness;

            float4 _PlayerPos;
            float4 _AimDir;
            float4 _CameraPos;
            float _OrthoSize;
            float _Aspect;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    uv.y = 1.0 - uv.y;
                #endif

                fixed4 source = tex2D(_MainTex, uv);

                float2 screenOffset =
                    (uv * 2.0 - 1.0) *
                    float2(
                        _OrthoSize * _Aspect,
                        _OrthoSize
                    );

                float2 worldPos =
                    _CameraPos.xy + screenOffset;

                float2 toPixel =
                    worldPos - _PlayerPos.xy;

                float distanceToPlayer = length(toPixel);

                float nearMask = 1.0 - smoothstep(
                    _NearRadius - _NearSoftness,
                    _NearRadius + _NearSoftness,
                    distanceToPlayer
                );

                float2 directionToPixel =
                    toPixel / max(distanceToPlayer, 0.0001);

                float aimDot = dot(
                    directionToPixel,
                    normalize(_AimDir.xy)
                );

                float halfAngle = _ConeAngle * 0.5;

                float outerCos = cos(radians(
                    min(
                        halfAngle + _ConeAngleSoftness,
                        179.0
                    )
                ));

                float innerCos = cos(radians(
                    max(
                        halfAngle - _ConeAngleSoftness,
                        0.1
                    )
                ));

                float angularMask = smoothstep(
                    outerCos,
                    innerCos,
                    aimDot
                );

                float radialMask = 1.0 - smoothstep(
                    _ConeLength - _ConeSoftness,
                    _ConeLength + _ConeSoftness,
                    distanceToPlayer
                );

                float coneMask = angularMask * radialMask;

                float visibility = max(
                    nearMask,
                    coneMask
                );

                float occlusion =
                    tex2D(_OcclusionTex, uv).r;

                visibility *= occlusion;
                visibility = saturate(visibility);

                // Darken the original scene.
                float darknessAmount =
                    _Darkness * (1.0 - visibility);

                source.rgb *= 1.0 - darknessAmount;

                // Composite eligible selective sprites after darkening the scene.
                fixed4 selective =
                    tex2D(_SelectiveTex, uv);

                // Clear-center monsters bypass only the wall mask; their color and
                // alpha still come from the normally sorted selective render.
                fixed4 unoccludedMonsterMask =
                    tex2D(_UnoccludedMonsterTex, uv);
                float bypassOcclusion = step(0.001, unoccludedMonsterMask.a);
                float spriteOcclusion = max(occlusion, bypassOcclusion);
                float selectiveAlpha =
                    saturate(selective.a * spriteOcclusion);

                source.rgb = lerp(
                    source.rgb,
                    selective.rgb,
                    selectiveAlpha
                );

                return source;
            }
            ENDCG
        }
    }

    Fallback Off
}
