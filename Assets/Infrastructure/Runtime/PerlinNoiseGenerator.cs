using AIR.Flume;
using Application.Interfaces;
using UnityEngine;

namespace Infrastructure.Runtime
{
    public class PerlinNoiseGenerator : Dependent, IHeightSource
    {
        private readonly int[,] _gradient = {
        {1,1,0}, {-1,1,0}, {1,-1,0}, {-1,-1,0},
        {1,0,1}, {-1,0,1}, {1,0,-1}, {-1,0,-1},
        {0,1,1}, {0,-1,1}, {0,1,-1}, {0,-1,-1},
        {1,1,0}, {0,-1,1}, {-1,1,0}, {0,-1,-1}};

        private int _period;

        private IValueSource _valueSource;
        private int _gridSize;
        private int _octaves;
        private float _persistence;

        public void Inject(
            ISeedService seedService,
            IValueSourceService valueSourceService)
        {
            _valueSource = valueSourceService.GetNewValueSource(seedService.Seed);
            _period = _gradient.GetLength(0);
            _gridSize = 64;
            _octaves = 3;
            _persistence = 0.5f;
        }

        public float GetUnitHeight(int x, int y)
        {
            return Perlin2D(x, y);
        }

        private float Perlin2D(int x, int y)
        {
            var valueSum = 0f;
            var freq = 1;
            var amp = 1.0f;
            var maxVal = 0f;

            for (var i = 0; i < _octaves; i++)
            {
                valueSum += Calculate2D(x * freq, y * freq) * amp;
                maxVal += amp;
                amp *= _persistence;
                freq *= 2;
            }

            return valueSum / maxVal;
        }

        private float Calculate2D(int x, int y)
        {
            var x0 = Mathf.FloorToInt((float)x / _gridSize) * _gridSize;
            var x1 = x0 + _gridSize;
            var xf = (float)(x - x0) / _gridSize;
            var y0 = Mathf.FloorToInt((float)y / _gridSize) * _gridSize;
            var y1 = y0 + _gridSize;
            var yf = (float)(y - y0) / _gridSize;

            var u = Fade(xf);
            var v = Fade(yf);

            var k0 = Gradient2D((int)(_valueSource.UnitFloat(x0, y0) * _period));
            var k1 = Gradient2D((int)(_valueSource.UnitFloat(x1, y0) * _period));
            var dx0 = Lerp(Dot(new float[2] { xf, yf }, k0), Dot(new float[2] { xf-1, yf }, k1), u);

            k0 = Gradient2D((int)(_valueSource.UnitFloat(x0, y1) * _period));
            k1 = Gradient2D((int)(_valueSource.UnitFloat(x1, y1) * _period));
            var dx1 = Lerp(Dot(new float[2] { xf, yf - 1 }, k0), Dot(new float[2] { xf - 1, yf - 1 }, k1), u);

            return (Lerp(dx0, dx1, v) * 1.5f + 1) / 2;
        }

        private int[] Gradient2D(int v)
        {
            var g = new int[2];
            g[0] = _gradient[v, 0];
            g[1] = _gradient[v, 1];

            return g;
        }

        private static float Fade(float t)
            => t * t * t * (t * (t * 6 - 15) + 10); // Smooth step (fade) function: 6t^5 - 15t^4 + 10t^3

        private static float Dot(float[] a, int[] b)
            => a[0] * b[0] + a[1] * b[1];

        private static float Lerp(float v0, float v1, float t)
            => (1 - t) * v0 + t * v1;
    }
}