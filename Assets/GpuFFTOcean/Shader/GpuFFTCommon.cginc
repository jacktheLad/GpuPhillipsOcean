#define PI UNITY_PI
#define G 9.81

uniform float N;
uniform float L;

float2 K(float m, float n)
{
    return float2(PI * (2 * m - N) / L, PI * (2 * n - N) / L);
}

float2 ComplexMul(float2 left, float2 right)
{
    float result_Realpart = (left.x * right.x) - (left.y * right.y);
    float result_Imaginarypart = (left.y * right.x) + (left.x * right.y);
    return float2(result_Realpart, result_Imaginarypart);
}