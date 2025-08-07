using Unity.Mathematics;

public static class ExtensionUtils
{
    public static float3 x0y(this float2 v) => new float3(v.x, 0, v.y);
    public static float3 x0y(this int2 v) => new float3(v.x, 0, v.y);

    public static int GetStableHashCode(this string str)
    {
        unchecked
        {
            var hash1 = 5381;
            var hash2 = hash1;

            for(var i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1 || str[i+1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i+1];
            }

            return hash1 + (hash2*1566083941);
        }
    }
}
