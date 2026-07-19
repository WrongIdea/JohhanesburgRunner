// Ubuntu Pulse energy shield: additive fresnel rim with an animated
// Tswana-inspired diamond band pattern, all evaluated procedurally so it
// needs no textures. Lives in Resources/ so Shader.Find survives Android
// build stripping. Single unlit pass (URP renders it as SRPDefaultUnlit);
// fresnel is computed per-vertex, which is plenty on the shield sphere and
// keeps the fragment cost mobile-friendly.
Shader "JoburgRunner/UbuntuPulseShield"
{
    Properties
    {
        _RimColor ("Rim Color", Color) = (0.35, 0.75, 1, 0.55)
        _PatternColor ("Pattern Color", Color) = (0.85, 0.97, 1, 0.5)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.6
        _GlowIntensity ("Glow Intensity", Range(0, 6)) = 1.8
        _Alpha ("Master Alpha", Range(0, 1)) = 1
        _Pulse ("Pulse Level", Range(0, 2)) = 1
        _ImpactFlash ("Impact Flash", Range(0, 1)) = 0
        _PatternBands ("Pattern Band Count", Range(2, 12)) = 5
        _PatternRepeat ("Pattern Repeat Around", Range(4, 40)) = 16
        _ScrollSpeed ("Pattern Scroll Speed", Range(-4, 4)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _RimColor;
            fixed4 _PatternColor;
            float _FresnelPower;
            float _GlowIntensity;
            float _Alpha;
            float _Pulse;
            float _ImpactFlash;
            float _PatternBands;
            float _PatternRepeat;
            float _ScrollSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fres : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.fres = pow(1.0 - saturate(abs(dot(worldNormal, viewDir))), _FresnelPower);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Diamond rows: alternate rows scroll in opposite directions
                // and are offset half a step, giving the woven geometric look.
                float row = i.uv.y * _PatternBands;
                float rowIndex = floor(row);
                float dir = fmod(rowIndex, 2.0) * 2.0 - 1.0;
                float u = i.uv.x * _PatternRepeat + _Time.y * _ScrollSpeed * dir + rowIndex * 0.5;
                float du = abs(frac(u) - 0.5) * 2.0;
                float dv = abs(frac(row) - 0.5) * 2.0;
                float d = du + dv;

                float fill = 1.0 - smoothstep(0.35, 0.6, d);
                float outline = smoothstep(0.6, 0.72, d) * (1.0 - smoothstep(0.78, 0.92, d));
                float pattern = saturate(outline * 1.2 + fill * 0.22);

                // The sphere UV pinches at the poles; fade the pattern out
                // there so it never smears into streaks.
                pattern *= smoothstep(0.03, 0.16, i.uv.y) * smoothstep(0.03, 0.16, 1.0 - i.uv.y);

                float flash = _ImpactFlash * _ImpactFlash;
                float3 col = _RimColor.rgb * (i.fres * _GlowIntensity)
                           + _PatternColor.rgb * pattern * (0.4 + 0.6 * i.fres) * _GlowIntensity;
                col *= _Pulse * (1.0 + flash * 2.5);

                float a = saturate((i.fres * _RimColor.a
                                    + pattern * _PatternColor.a * 0.6
                                    + flash * 0.35) * _Alpha);
                return fixed4(col, a);
            }
            ENDCG
        }
    }

    Fallback Off
}
