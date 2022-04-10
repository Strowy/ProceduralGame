using UnityEngine;

public class PerlinNoise
{
    // Gradient permutation array and its period
    private readonly int[,] gradient = {
        {1,1,0}, {-1,1,0}, {1,-1,0}, {-1,-1,0},
        {1,0,1}, {-1,0,1}, {1,0,-1}, {-1,0,-1},
        {0,1,1}, {0,-1,1}, {0,1,-1}, {0,-1,-1},
        {1,1,0}, {0,-1,1}, {-1,1,0}, {0,-1,-1}};

    private readonly int period;

    // Settings for Perlin noise generation
    private PseudoRandomGenerator rng;
    private int gridSize;
    private int octaves;
    private float persistence;

    // Constructor
    public PerlinNoise(int seed)
    {
        period = gradient.GetLength(0);
        rng = new PseudoRandomGenerator(seed);
        gridSize = 64;
        octaves = 3;
        persistence = 0.5f;
    }

    // Returns a bounded float in [0, 1]
    public float Perlin2D(int x, int y)
    {
        // Values for multi-octave calculation
        float valueSum, amp, maxVal;
        int freq;
        
        valueSum = 0f;
        freq = 1;
        amp = 1.0f;
        maxVal = 0f;

        for (int i = 0; i < octaves; i++)
        {
            valueSum += Calculate2D(x * freq, y * freq) * amp;
            maxVal += amp;
            amp *= persistence;
            freq *= 2;
        }

        // return float in range [0, 1]: sum / maximum possible value
        return valueSum / maxVal;
    }

    private float Calculate2D(int x, int y)
    {
        // Unit square values and distance
        int x0, x1, y0, y1;
        float xf, yf, u, v, dx0, dx1;
        int[] k0, k1;

        x0 = Mathf.FloorToInt((float)x / gridSize) * gridSize;
        x1 = x0 + gridSize;
        xf = (float)(x - x0) / gridSize;
        y0 = Mathf.FloorToInt((float)y / gridSize) * gridSize;
        y1 = y0 + gridSize;
        yf = (float)(y - y0) / gridSize;

        // Apply smoothstep
        u = Fade(xf);
        v = Fade(yf);

        // Take 'random' unit gradients and interpolate on x then y on unit square
        k0 = Gradient2D((int)(rng.UnitFloat(x0, y0) * period));
        k1 = Gradient2D((int)(rng.UnitFloat(x1, y0) * period));
        dx0 = Lerp(Dot(new float[2] { xf, yf }, k0), Dot(new float[2] { xf-1, yf }, k1), u);

        k0 = Gradient2D((int)(rng.UnitFloat(x0, y1) * period));
        k1 = Gradient2D((int)(rng.UnitFloat(x1, y1) * period));
        dx1 = Lerp(Dot(new float[2] { xf, yf - 1 }, k0), Dot(new float[2] { xf - 1, yf - 1 }, k1), u);

        // interpolate on y, set value in range [0 - 1]
        return (Lerp(dx0, dx1, v) * 1.5f + 1) / 2;
    }

    // Smoothstep (fade) function: 6t^5 - 15t^4 + 10t^3
    private float Fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }

    private int[] Gradient2D(int v)
    {   
        int[] g = new int[2];
        g[0] = gradient[v, 0];
        g[1] = gradient[v, 1];

        return g;
    }

    private float Dot(float[] a, int[] b) { return a[0] * b[0] + a[1] * b[1]; }

    private float Lerp(float v0, float v1, float t) { return (1 - t) * v0 + t * v1; }
}