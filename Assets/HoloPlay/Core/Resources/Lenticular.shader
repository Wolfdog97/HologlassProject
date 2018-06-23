//Copyright 2017 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

Shader "HoloPlay/Lenticular" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}
		SubShader{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 300

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass{
		CGPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
// #include "./HoloPlayInclude.cginc"

		struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	float4 _MainTex_ST;

	v2f vert(appdata_base v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
		return o;
	}

	sampler2D _MainTex;
	float pitch;
	float tilt;
	float center;
	float invView;
	float flipX;
	float flipY;
	float subp;
	int ri;
	int bi;
	float4 tile;
	float numViews;

	// uvz: fullscreen x and y, plus z is the view number in int
	// tile: x: tiles across, y: tiles down, z: portion across, w: portion down
	float2 texArr(float3 uvz) 
	{
		// decide which section to take from based on the z.
		float z = floor(uvz.z * tile.x * tile.y);
		// float x = fmod(z, 1.0) + (uvz.x / tile.x);
		float x = (fmod(z, tile.x) + uvz.x) / tile.x;
		float y = (floor(z / tile.x) + uvz.y) / tile.y;
		return saturate(float2(x, y)) * tile.zw;
	}

	float4 frag(v2f IN) : COLOR{
		float4 rgb[3];
		float3 nuv = float3(IN.uv.xy, 0.0);
		nuv.x = (1.0 - flipX) * nuv.x + flipX * (1.0 - nuv.x);
		nuv.y = (1.0 - flipY) * nuv.y + flipY * (1.0 - nuv.y);

		for (int i; i < 3; i++) {
			nuv.z = (nuv.x + i * subp + IN.uv.y * tilt) * pitch - center;
			// adding ceil pitch+center doesn't do anything but add enough pad
			// to handle a possibly negative nuv.z
			nuv.z = fmod(nuv.z + ceil(abs(pitch * tilt)) + 1, 1.0);
			nuv.z = (1.0 - invView) * nuv.z + invView * (1.0 - nuv.z);
			rgb[i] = tex2D(_MainTex, texArr(nuv));
		}

		return float4(rgb[ri].r, rgb[1].g, rgb[bi].b, 1);
	}
		ENDCG
	}
	}
		FallBack Off
}