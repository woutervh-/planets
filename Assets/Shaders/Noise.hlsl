#ifndef NOISE_INCLUDED
#define NOISE_INCLUDED

sampler2D _Gradients2D;
sampler2D _Permutation2D;
float _Strength;
float _Frequency;
float _Lacunarity;
float _Persistence;
int _Octaves;

float3 fade(float3 t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float3 fadeDerivative(float3 t) {
    return 30.0 * t * t * (t * (t - 2.0) + 1.0);
}

float3 gradient(float t) {
    return tex2Dlod(_Gradients2D, float4(t, 0, 0, 0)).xyz * 2.0 - 1.0;
}

float4 perlin(float3 p) {
    float3 ip = fmod(floor(p), 256.0) / 256.0;
    float3 fp = p - floor(p);
    float3 u = fade(fp);
    float3 du = fadeDerivative(fp);
    float4 permutation = tex2Dlod(_Permutation2D, float4(ip.xy, 0, 0));
    float aa = permutation.x + ip.z;
    float ab = permutation.y + ip.z;
    float ba = permutation.z + ip.z;
    float bb = permutation.w + ip.z;
    
    float3 g000 = gradient(aa);
    float3 g100 = gradient(ba);
    float3 g010 = gradient(ab);
    float3 g110 = gradient(bb);
    float3 g001 = gradient(aa + 1.0 / 256.0);
    float3 g101 = gradient(ba + 1.0 / 256.0);
    float3 g011 = gradient(ab + 1.0 / 256.0);
    float3 g111 = gradient(bb + 1.0 / 256.0);
    
    float v000 = dot(g000, fp + float3(0, 0, 0));
    float v100 = dot(g100, fp + float3(-1, 0, 0));
    float v010 = dot(g010, fp + float3(0, -1, 0));
    float v110 = dot(g110, fp + float3(-1, -1, 0));
    float v001 = dot(g001, fp + float3(0, 0, -1));
    float v101 = dot(g101, fp + float3(-1, 0, -1));
    float v011 = dot(g011, fp + float3(0, -1, -1));
    float v111 = dot(g111, fp + float3(-1, -1, -1));
    
    float a = v000;
    float b = v100 - v000;
    float c = v010 - v000;
    float d = v001 - v000;
    float e = v110 - v010 - v100 + v000;
    float f = v101 - v001 - v100 + v000;
    float g = v011 - v001 - v010 + v000;
    float h = v111 - v011 - v101 + v001 - v110 + v010 + v100 - v000;
    
    float3 da = g000;
    float3 db = g100 - g000;
    float3 dc = g010 - g000;
    float3 dd = g001 - g000;
    float3 de = g110 - g010 - g100 + g000;
    float3 df = g101 - g001 - g100 + g000;
    float3 dg = g011 - g001 - g010 + g000;
    float3 dh = g111 - g011 - g101 + g001 - g110 + g010 + g100 - g000;
    
    float v = a + b * u.x + (c + e * u.x) * u.y + (d + f * u.x + (g + h * u.x) * u.y) * u.z;
    float3 dv = da + db * u.x + (dc + de * u.x) * u.y + (dd + df * u.x + (dg + dh * u.x) * u.y) * u.z;
    dv.x += du.x * (b + e * u.y + (f + h * u.y) * u.z);
    dv.y += du.y * (c + e * u.x + (g + h * u.x) * u.z);
    dv.z += du.z * (d + f * u.x + (g + h * u.x) * u.y);
    
    return float4(dv, v);
}

float4 noise(float3 p) {
    float strength = _Strength;
    float frequency = _Frequency;
    float4 sum = perlin(p * frequency) * strength;
    sum.xyz *= frequency;
    for (int i=0; i<_Octaves; i++) {
        strength *= _Persistence;
        frequency *= _Lacunarity;
        float4 value = perlin(p * frequency) * strength;
        sum.xyz += value.xyz * frequency;
        sum.w += value.w;
    }
    // sum.w += 1.0;
    return sum;
}

float3 finiteDifferenceGradient(float3 p, float h) {
    float f = noise(p).w;
    float fx = noise(p + float3(h, 0, 0)).w;
    float fy = noise(p + float3(0, h, 0)).w;
    float fz = noise(p + float3(0, 0, h)).w;

    return float3((fx - f) / h, (fy - f) / h, (fz - f) / h);
}

#endif
