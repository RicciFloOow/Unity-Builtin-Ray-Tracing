#ifndef PBRHELPERLIB_INCLUDE
#define PBRHELPERLIB_INCLUDE

//ref: https://en.wikipedia.org/wiki/Schlick%27s_approximation
float SchlickFresnelSpecularReflection(float cosTheta, float ior)
{
    float r0 = ((1 - ior) / (1 + ior));
    r0 *= r0;
    float u = 1 - cosTheta;
    float u2 = u * u;
    u2 *= u2;
    return r0 + (1 - r0) * u2 * u;
}

float3 SchlickFresnelSpecularReflectionOpaque(float cosTheta, float3 f0)
{
    float u = 1 - cosTheta;
    float u2 = u * u;
    u2 *= u2;
    return f0 + (1 - f0) * u2 * u;
}
#endif