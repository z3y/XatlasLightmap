Shader "Hidden/z3y/LMBlur"
{
    Properties
    {
        _MainTex ("Lightmap", 2D) = "white" {}

        _Mask ("Blur Mask", 2D) = "white" {}

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float2 _lightmapRes;

            sampler2D _Mask;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv2;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 lightmap = tex2D(_MainTex, i.uv);
                //fixed4 mask = tex2D(_Mask, i.uv);

                //lightmap*= mask;

                float Pi = UNITY_PI*2; // Pi*2
                
                // GAUSSIAN BLUR SETTINGS {{{
                float Directions = 32.0; // BLUR DIRECTIONS (Default 16.0 - More is better but slower)
                float Quality = 6.0; // BLUR QUALITY (Default 4.0 - More is better but slower)
                float Size = 2.0; // BLUR SIZE (Radius)
                // GAUSSIAN BLUR SETTINGS }}}
            
                float2 Radius = _lightmapRes.xy * Size;
                
                // Normalized pixel coordinates (from 0 to 1)
                float2 uv = i.uv;
                // Pixel colour
                float4 lightmap = tex2D(_MainTex, uv);
                float4 Color = lightmap;
                float mask = tex2D(_Mask, uv).r;
                
                // Blur calculations
                for( float d=0.0; d<Pi; d+=Pi/Directions)
                {
                    for(float c=1.0/Quality; c<=1.0; c+=1.0/Quality)
                    {
                        float2 offset = float2(cos(d),sin(d))*Radius*c;
                        float maskOffset = tex2D(_Mask, uv + offset).r;
                        Color += tex2D(_MainTex, uv + offset * maskOffset);		
                    }
                }
                
                // Output to screen
                Color /= Quality * Directions - Directions;

                Color = lerp(lightmap, Color, saturate(mask * mask));


                return Color;
            }
            ENDCG
        }
    }
}
