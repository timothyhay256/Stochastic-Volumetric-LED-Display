using Unity.Mathematics;

namespace Lasp
{
    //
    // Five-band filter with a biquad IIR filter
    //
    struct MultibandFilter
    {
        float4 _a0, _a1, _a2;
        float  _b1, _b2;
        float4 _z1, _z2;

        static readonly float4 _xmask = new float4(0, 1, 1, 1);

        public void SetParameters(float sampleRate, float filterFc, float filterQ)
        {
            // Calculate cutoff frequencies for each band
            float nyquist = sampleRate * 0.5f; // Nyquist frequency
            float bandWidth = nyquist / 5.0f; // Divide into 5 bands

            // 0: Bypass
            _a0.x = 1;
            _a1.x = 0;
            _a2.x = 0;

            // 1: Low band (0 - Fc1)
            float Fc1 = bandWidth;
            SetBandCoefficients(ref _a0.y, ref _a1.y, ref _a2.y, Fc1, sampleRate, filterQ);

            // 2: Low-mid band (Fc1 - Fc2)
            float Fc2 = 2 * bandWidth;
            SetBandCoefficients(ref _a0.z, ref _a1.z, ref _a2.z, Fc2, sampleRate, filterQ);

            // 3: Mid band (Fc2 - Fc3)
            float Fc3 = 3 * bandWidth;
            SetBandCoefficients(ref _a0.w, ref _a1.w, ref _a2.w, Fc3, sampleRate, filterQ);

            // 4: Mid-high band (Fc3 - Fc4)
            float Fc4 = 4 * bandWidth;
            SetBandCoefficients(ref _a0.w, ref _a1.w, ref _a2.w, Fc4, sampleRate, filterQ);

            // 5: High band (Fc4 - Nyquist)
            SetHighBandCoefficients(ref _a0.w, ref _a1.w, ref _a2.w, sampleRate);
        }

        private void SetBandCoefficients(ref float a0, ref float a1, ref float a2, float Fc, float sampleRate, float Q)
        {
            var K = math.tan((float)math.PI * Fc / sampleRate);
            var norm = 1 / (1 + K * K / Q + K * K);

            a0 = K * K * norm;
            a1 = 2 * a0;
            a2 = a0;

            _b1 = 2 * (K * K - 1) * norm;
            _b2 = (1 - K / Q + K * K) * norm;
        }

        private void SetHighBandCoefficients(ref float a0, ref float a1, ref float a2, float sampleRate)
        {
            var K = 0f; // Highpass does not use K
            var norm = 1 / (1 + K * K);

            a0 = norm;
            a1 = -2 * a0;
            a2 = a0;

            _b1 = 2 * (K * K - 1) * norm;
            _b2 = (1 - K / 1 + K * K) * norm;
        }

        public float4 FeedSample(float i)
        {
            var o = _a0 * i + _z1 * _xmask;
            _z1 = _a1 * i + _z2 - o * _b1;
            _z2 = _a2 * i - o * _b2;
            return o;
        }
    }
}
