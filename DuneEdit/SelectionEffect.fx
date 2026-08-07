/// <description>A simple color blending shader for WPF.</description>
/// <target>WPF</target>
/// <profile>ps_2_0</profile>

//-----------------------------------------------------------------------------
// Constants
//-----------------------------------------------------------------------------

/// <summary>The brightness offset.</summary>
/// <type>Color</type>
/// <defaultValue>0,0,0,0</defaultValue>
float4 GlowColor : register(c0);

/// <summary>The center of the blur.</summary>
/// <defaultValue>0.5,0.5</defaultValue>
/// <minValue>0</minValue>
/// <maxValue>1</maxValue>
float2 Center : register(c1);

/// <summary>The amount of blur.</summary>
/// <defaultValue>0.15</defaultValue>
/// <minValue>0</minValue>
/// <maxValue>0.5</maxValue>
float BlurAmount : register(c2);

/// <summary>The size of the glowing halo.</summary>
/// <defaultValue>0.1</defaultValue>
/// <minValue>0</minValue>
/// <maxValue>0.5</maxValue>
float GlowSize : register(c3);

/// <summary>The intensity of inner (non-halo) glow.</summary>
/// <defaultValue>0.1</defaultValue>
/// <minValue>0</minValue>
/// <maxValue>1.0</maxValue>
float InnerGlow : register(c4);

//-----------------------------------------------------------------------------
// Samplers
//-----------------------------------------------------------------------------

/// <summary>The implicit input sampler passed into the pixel shader by WPF.</summary>
/// <samplingMode>Auto</samplingMode>
sampler2D Input : register(s0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
    const float steps = 10;
	float c = 0;
	float4 color = tex2D(Input, uv);
	uv -= Center;

	for (int i = 0; i < steps; i++)
    {
		float scale = 1.0 + BlurAmount * (i / (steps - 1)) - GlowSize;
		c += tex2D(Input, uv * scale + Center).a;
	}
   
	c /= steps;
	color.rgb = lerp(color.rgb, GlowColor.rgb * c, saturate(c - color.a + InnerGlow));
	color.a = max(color.a, c);
	color.rgb *= color.a;

	return color;
}
