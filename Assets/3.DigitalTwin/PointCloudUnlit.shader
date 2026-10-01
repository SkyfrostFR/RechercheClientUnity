// Unlit vertex-colour shader for MeshTopology.Points.
//
// URP will not draw a shader that has no UniversalPipeline pass, so a plain CG unlit
// shader from the built-in pipeline renders nothing here — this project is on URP 17.
//
// Points rasterise at one pixel. For a 640x480 depth frame that is dense enough to read
// as a surface; if you want fatter points you need a geometry stage or instanced quads.
Shader "Tiago/PointCloudUnlit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4  color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }
}
