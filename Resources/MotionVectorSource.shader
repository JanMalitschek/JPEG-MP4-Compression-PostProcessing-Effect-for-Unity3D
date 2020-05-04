Shader "Hidden/MotionVectorSource"
{
    //We don't need any properties, not even an input texture as this shader's only purpose is to sample
    //the motion vector uniform texture
    Properties{}
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            //Nothing has changed here at all
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            //Make sure to define this before using it
            //This is a uniform provided by the engine which contains the main cameras motion vector texture
            sampler2D _CameraMotionVectorsTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                //Sample the motion vectors, no fancy maths required
                return tex2D(_CameraMotionVectorsTexture, i.uv);
            }
            ENDCG
        }
    }
}
