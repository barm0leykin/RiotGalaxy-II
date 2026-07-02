// Bloom post-process для десктопа: выделение ярких зон (Extract) + гауссово размытие (Blur).
// Композиция (сцена + размытые яркие зоны) делается аддитивным SpriteBatch в коде — отдельный
// combine-шейдер не нужен. Совместимо со SpriteBatch: параметр MatrixTransform заполняет сам
// SpriteBatch; текстура биндится в сэмплер s0.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 MatrixTransform;

sampler2D TextureSampler : register(s0);

// Extract
float Threshold = 0.5;

// Blur (веса/смещения задаём из кода)
#define SAMPLE_COUNT 15
float2 SampleOffsets[SAMPLE_COUNT];
float  SampleWeights[SAMPLE_COUNT];

struct VOut
{
    float4 pos : SV_POSITION;
    float4 col : COLOR0;
    float2 uv  : TEXCOORD0;
};

VOut VS(float4 pos : POSITION0, float4 col : COLOR0, float2 uv : TEXCOORD0)
{
    VOut o;
    o.pos = mul(pos, MatrixTransform);
    o.col = col;
    o.uv  = uv;
    return o;
}

// Оставляем только то, что ярче порога, и растягиваем в 0..1.
float4 ExtractPS(VOut i) : COLOR0
{
    float4 c = tex2D(TextureSampler, i.uv);
    return saturate((c - Threshold) / (1.0 - Threshold));
}

// Гауссово размытие в одном направлении (offsets задают горизонталь/вертикаль).
float4 BlurPS(VOut i) : COLOR0
{
    float4 c = 0;
    for (int k = 0; k < SAMPLE_COUNT; k++)
        c += tex2D(TextureSampler, i.uv + SampleOffsets[k]) * SampleWeights[k];
    return c;
}

technique Extract
{
    pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL ExtractPS(); }
}

technique Blur
{
    pass P0 { VertexShader = compile VS_SHADERMODEL VS(); PixelShader = compile PS_SHADERMODEL BlurPS(); }
}
