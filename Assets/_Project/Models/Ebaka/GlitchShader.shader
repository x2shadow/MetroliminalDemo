Shader "Custom/WorldSpaceHologram"

{

    Properties

    {

        [Enum(None, 0, Horizontal, 1, Vertical, 2, Both, 3)] _Direction ("Line Direction", Int) = 0

        _MainTex ("Texture", 2D) = "white" {}

        _Tiling ("Tiling", Float) = 10.0

        _Speed ("Speed", Float) = 1.0

        _HologramColor ("Hologram Color", Color) = (0.0, 1.0, 1.0, 1.0) [HDR]

        _FresnelColor ("Fresnel Color", Color) = (1.0, 1.0, 1.0, 1.0) [HDR]

        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 2.0



        [Toggle] _GlitchToggle ("Enable Glitch", Float) = 0

        [Enum(None, 0, Horizontal, 1, Vertical, 2, Both, 3)] _GlitchDirection ("Glitch Direction", Int) = 0

        _GlitchIntensity ("Glitch Intensity", Range(0.0, 1.0)) = 0.5

        _BlockSize ("Block Size", Range(0.1, 10.0)) = 1.0

    }

    SubShader

    {

        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        LOD 200



        Pass

        {

            Name "ForwardLit"

            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite On

            HLSLPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"



            struct Attributes

            {

                float4 positionOS : POSITION;

                float3 normalOS : NORMAL;

                float2 uv : TEXCOORD0;

            };



            struct Varyings

            {

                float4 positionHCS : SV_POSITION;

                float3 worldPos : TEXCOORD0;

                float3 worldNormal : TEXCOORD1;

                float3 viewDir : TEXCOORD2;

                float2 uv : TEXCOORD3;

                float flickerAlpha : TEXCOORD4;

                float3 flickerRGB : TEXCOORD5;

            };



            sampler2D _MainTex;

            float4 _HologramColor;

            float4 _FresnelColor;

            float _Tiling;

            float _Speed;

            int _Direction;

            float _FresnelPower;



            float _GlitchToggle;

            int _GlitchDirection;

            float _GlitchIntensity;

            float _BlockSize;



            // Random function

            float random(float2 st)

            {

                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);

            }



            Varyings vert (Attributes input)

            {

                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS);

                output.worldPos = TransformObjectToWorld(input.positionOS);

                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);

                float3 worldViewDir = GetWorldSpaceViewDir(input.positionOS.xyz);

                output.viewDir = worldViewDir;

                output.uv = input.uv;

                output.flickerAlpha = 1.0;

                output.flickerRGB = float3(1.0, 1.0, 1.0);



                // Apply blocky glitch effect in vertex shader

                if (_GlitchToggle == 1.0)

                {

                    float blockSize = 1.0 / max(0.001, _BlockSize);

                    float2 blockPos = floor(input.positionOS.xy / blockSize) * blockSize;



                    float glitchValue = random(float2(_Time.y, blockPos.y)) * 10.0;

                    if (frac(glitchValue) < _GlitchIntensity)

                    {

                        float glitchOffset = (random(float2(blockPos.y, glitchValue)) - 0.5) * blockSize;

                        if (_GlitchDirection == 1 || _GlitchDirection == 3) // Horizontal or Both directions

                        {

                            output.positionHCS.x += glitchOffset;

                        }

                        if (_GlitchDirection == 2 || _GlitchDirection == 3) // Vertical or Both directions

                        {

                            output.positionHCS.y += glitchOffset;

                        }



                        // Apply drastic flicker effect for glitch blocks

                        output.flickerAlpha = random(blockPos) * 0.5 + 0.5; // Random flicker alpha

                        output.flickerRGB = float3(random(blockPos + 1.0), random(blockPos + 2.0), random(blockPos + 3.0));

                    }

                }



                return output;

            }



            half4 frag (Varyings input) : SV_Target

            {

                float time = _Time.y * _Speed;

                float linePattern = 1.0;



                if (_Direction == 1) // Horizontal lines

                {

                    linePattern = step(0.5, frac(input.worldPos.y * _Tiling + time));

                }

                else if (_Direction == 2) // Vertical lines

                {

                    linePattern = step(0.5, frac(input.worldPos.x * _Tiling + time));

                }

                else if (_Direction == 3) // Both directions

                {

                    float horizontal = step(0.5, frac(input.worldPos.y * _Tiling + time));

                    float vertical = step(0.5, frac(input.worldPos.x * _Tiling + time));

                    linePattern = max(horizontal, vertical);

                }



                // Calculate Fresnel effect

                float3 normal = normalize(input.worldNormal);

                float3 viewDir = normalize(input.viewDir);

                float fresnel = pow(1.0 - dot(normal, viewDir), _FresnelPower);



                // Fresnel color blending without affecting alpha

                float4 fresnelColor = float4(_FresnelColor.rgb * fresnel, 1.0);



                // Sample the texture with glitch UV displacement

                float2 glitchUV = input.uv;

                if (_GlitchToggle == 1.0)

                {

                    float blockSize = 1.0 / max(0.001, _BlockSize);

                    float2 blockPos = floor(glitchUV / blockSize) * blockSize;



                    float glitchValue = random(float2(_Time.y, blockPos.y)) * 10.0;

                    if (frac(glitchValue) < _GlitchIntensity)

                    {

                        float glitchOffset = (random(float2(blockPos.y, glitchValue)) - 0.5) * blockSize;

                        if (_GlitchDirection == 1 || _GlitchDirection == 3) // Horizontal or Both directions

                        {

                            glitchUV.x += glitchOffset;

                        }

                        if (_GlitchDirection == 2 || _GlitchDirection == 3) // Vertical or Both directions

                        {

                            glitchUV.y += glitchOffset;

                        }

                    }

                }



                half4 texColorR = tex2D(_MainTex, glitchUV + float2(0.002, 0));

                half4 texColorG = tex2D(_MainTex, glitchUV);

                half4 texColorB = tex2D(_MainTex, glitchUV - float2(0.002, 0));



                half4 texColor = half4(texColorR.r, texColorG.g, texColorB.b, texColorG.a);



                // Apply color, texture, Fresnel effect, and flicker alpha

                half4 color = half4(_HologramColor.rgb * input.flickerRGB * texColor.rgb, _HologramColor.a * linePattern * input.flickerAlpha);

                color.rgb += fresnelColor.rgb;



                return color;

            }

            ENDHLSL

        }

    }

    //FallBack "Universal Render Pipeline/Lit"

}