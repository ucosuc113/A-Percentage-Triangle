Shader "Custom/RainbowWithFlash"
{
    Properties
    {
        _CycleSpeed ("Cycle Speed", Float) = 0.1
        _TimeOffset ("Time Offset", Float) = 0.0

        _FlashDuration ("Flash Duration", Float) = 0.2
        _FlashProgress ("Flash Progress", Range(0,1)) = 0.0
        _FlashColor1 ("Flash Color 1", Color) = (1,0,0,1)
        _FlashColor2 ("Flash Color 2", Color) = (0,1,0,1)
        _FlashColor3 ("Flash Color 3", Color) = (0,0,1,1)
        _FlashColor4 ("Flash Color 4", Color) = (1,1,0,1)

        _Brightness ("Brightness", Range(0,1)) = 1.0
        _PlayerExists ("Player Exists", Float) = 1.0
        _ToBlackProgress ("To Black Progress", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _CycleSpeed;
            float _TimeOffset;

            float _FlashDuration;
            float _FlashProgress;
            fixed4 _FlashColor1;
            fixed4 _FlashColor2;
            fixed4 _FlashColor3;
            fixed4 _FlashColor4;

            float _Brightness;
            float _PlayerExists;
            float _ToBlackProgress;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float3 HSV2RGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _CycleSpeed + _TimeOffset;
                float hue = frac(t);
                float3 rainbowCol = HSV2RGB(float3(hue, 1.0, 1.0));

                if (_FlashProgress > 0)
                {
                    float phase = frac(_FlashProgress * 4.0);
                    fixed4 fc;
                    if (phase < 0.25) fc = _FlashColor1;
                    else if (phase < 0.5) fc = _FlashColor2;
                    else if (phase < 0.75) fc = _FlashColor3;
                    else fc = _FlashColor4;

                    float mixFactor = smoothstep(0.0, 1.0, _FlashProgress);
                    rainbowCol = lerp(rainbowCol, fc.rgb, mixFactor);
                }

                if (_PlayerExists <= 0.0)
                {
                    rainbowCol = lerp(rainbowCol, float3(0,0,0), saturate(_ToBlackProgress));
                }

                return fixed4(rainbowCol * _Brightness, 1.0);
            }
            ENDCG
        }
    }
}
