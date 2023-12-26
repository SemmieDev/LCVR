Shader "LCVR/StereoscopicImage" {
   Properties {
      _MainTex("Unused, required", 2D) = "white" {}
      _LeftEyeTex("Left Eye Texture", 2D) = "white" {}
      _RightEyeTex("Right Eye Texture", 2D) = "white" {}
   }

   SubShader {
      Tags {
         "RenderType" = "Opaque"
      }

      Pass {
         CGPROGRAM

         #pragma vertex vert
         #pragma fragment frag

         sampler2D _LeftEyeTex;
         sampler2D _RightEyeTex;

         #include "UnityCG.cginc"

         struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
         };

         struct v2f {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
         };

         v2f vert(appdata v) {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            float4 pos = UnityObjectToClipPos(v.vertex);

            // This makes sure that the image is a fullscreen overlay. Very jank, but it works.
            o.vertex = float4(pos.x < 0 ? -1 : 1, pos.y < 0 ? -1 : 1, 0, 1);
            o.uv = v.uv;

            return o;
         }

         fixed4 frag(v2f i) : SV_Target {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

            fixed4 color;

            if (unity_StereoEyeIndex == 0) {
               color = tex2D(_LeftEyeTex, i.uv);
            } else {
               color = tex2D(_RightEyeTex, i.uv);
            }

            return color;
         }

         ENDCG
      }
   }
}