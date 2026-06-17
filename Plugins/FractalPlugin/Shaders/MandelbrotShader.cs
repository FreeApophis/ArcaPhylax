using ComputeSharp;

namespace FractalPlugin.Shaders;

/// <summary>
/// GPU compute shader that renders the Mandelbrot set into an RGBA texture.
/// One GPU thread is dispatched per pixel; coordinates map to the complex plane via
/// <c>c = center + (pixel - half) * scale</c>, mirroring the original CPU renderer.
/// </summary>
[ThreadGroupSize(DefaultThreadGroupSizes.XY)]
[GeneratedComputeShaderDescriptor]
internal readonly partial struct MandelbrotShader(
    ReadWriteTexture2D<Rgba32, float4> texture,
    float centerX,
    float centerY,
    float scale,
    int maxIterations) : IComputeShader
{
    public void Execute()
    {
        int x = ThreadIds.X;
        int y = ThreadIds.Y;

        if (x >= DispatchSize.X || y >= DispatchSize.Y)
        {
            return;
        }

        float halfW = DispatchSize.X * 0.5f;
        float halfH = DispatchSize.Y * 0.5f;

        float cr = centerX + (x - halfW) * scale;
        float ci = centerY + (y - halfH) * scale;

        float zr = 0f, zi = 0f, zr2 = 0f, zi2 = 0f;

        int it = 0;
        while (it < maxIterations && (zr2 + zi2) <= 4f)
        {
            zi = 2f * zr * zi + ci;
            zr = zr2 - zi2 + cr;

            zr2 = zr * zr;
            zi2 = zi * zi;
            it++;
        }

        texture[x, y] = it >= maxIterations
            ? new float4(0f, 0f, 0f, 1f)
            : Color(it / (float)maxIterations);
    }

    /// <summary>Maps a normalised iteration count in [0, 1) onto the gradient used by the CPU renderer.</summary>
    private static float4 Color(float t)
        => new(1f - Hlsl.Pow(1f - t, 3f), 1f - Hlsl.Pow(1f - t, 2f), Hlsl.Sqrt(t), 1f);
}
