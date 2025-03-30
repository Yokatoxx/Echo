#ifndef POINT_CLOUD_FUNCTIONS_INCLUDED
#define POINT_CLOUD_FUNCTIONS_INCLUDED

float3 ChaosOffset(float3 position, float chaos)
{
    float noiseX = frac(sin(dot(position.xyz, float3(12.9898,78.233, 54.53))) * 43758.5453);
    float noiseY = frac(sin(dot(position.yzx, float3(39.346,11.135,92.654))) * 12456.2311);
    float noiseZ = frac(sin(dot(position.zxy, float3(81.25,19.76,33.88))) * 93821.1234);
    float3 offset = float3(noiseX, noiseY, noiseZ) * 2.0 - 1.0;
    return position + offset * chaos;
}

#endif