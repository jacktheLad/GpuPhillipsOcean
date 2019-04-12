using UnityEngine;
using System.Collections;


namespace SIF
{
    public class GpuFFT
    {
        int Size;
        float FSize;
        int Passes;
        public Texture2D ButterflyLookupTable = null;
        Material FFTMat;

        public GpuFFT(int size)
        {
            if (size > 256)
            {
                Debug.Log("GpuFFT::GpuFFT - fourier grid size must not be greater than 256, changing to 256");
                size = 256;
            }

            if (!Mathf.IsPowerOfTwo(size))
            {
                Debug.Log("GpuFFT::GpuFFT - fourier grid size must be pow2 number, changing to nearest pow2 number");
                size = Mathf.NextPowerOfTwo(size);
            }

            FFTMat = new Material(Shader.Find("WaterAllInOne/GpuFFTOcean/FFT"));

            Size = size; //must be pow2 num
            FSize = Size;
            Passes = (int)(Mathf.Log(FSize) / Mathf.Log(2.0f));

            ComputeButterflyLookupTable();
        }

        int BitReverse(int i)
        {
            int j = i;
            int Sum = 0;
            int W = 1;
            int M = Size / 2;
            while (M != 0)
            {
                j = ((i & M) > M - 1) ? 1 : 0;
                Sum += j * W;
                W *= 2;
                M /= 2;
            }
            return Sum;
        }

        Texture2D MakeLut()
        {
            Texture2D tex = new Texture2D(Size, Passes, TextureFormat.RGBAFloat, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        void ComputeButterflyLookupTable()
        {
            ButterflyLookupTable = MakeLut();
            for (int i = 0; i < Passes; i++)
            {
                int nBlocks = (int)Mathf.Pow(2, Passes - 1 - i);
                int nHInputs = (int)Mathf.Pow(2, i);

                for (int j = 0; j < nBlocks; j++)
                {
                    for (int k = 0; k < nHInputs; k++)
                    {
                        int i1, i2, j1, j2;
                        if (i == 0)
                        {
                            i1 = j * nHInputs * 2 + k;
                            i2 = j * nHInputs * 2 + nHInputs + k;
                            j1 = BitReverse(i1);
                            j2 = BitReverse(i2);
                        }
                        else
                        {
                            i1 = j * nHInputs * 2 + k;
                            i2 = j * nHInputs * 2 + nHInputs + k;
                            j1 = i1;
                            j2 = i2;
                        }

                        float wr = Mathf.Cos(2.0f * Mathf.PI * (k * nBlocks) / FSize);
                        float wi = Mathf.Sin(2.0f * Mathf.PI * (k * nBlocks) / FSize);

                        Color encodeI1Color = new Color(j1 / (FSize - 1.0f), j2 / (FSize - 1.0f), wr, wi);
                        Color encodeI2Color = new Color(j1 / (FSize - 1.0f), j2 / (FSize - 1.0f), -wr, -wi);

                        ButterflyLookupTable.SetPixel(i1, i, encodeI1Color);
                        ButterflyLookupTable.SetPixel(i2, i, encodeI2Color);
                    }
                }

                ButterflyLookupTable.Apply();
            }
        }

        public int PerformFFT(int startIdx, RenderTexture[][] datas)
        {
            int i;
            int idx = 0; int idx1;

            int j = startIdx;
            int fftCount = datas.Length;

            FFTMat.SetTexture("_ButterFlyLookUp", ButterflyLookupTable);

            for (i = 0; i < Passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;
                FFTMat.SetFloat("_V", (i + 0.5f) / Passes);
                for (int k = 0; k < fftCount; k++)
                {
                    FFTMat.SetTexture("_ReadBuffer", datas[k][idx1]);
                    Graphics.Blit(null, datas[k][idx], FFTMat, 0);
                }
            }

            for (i = 0; i < Passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;
                FFTMat.SetFloat("_V", (i + 0.5f) / Passes);
                for (int k = 0; k < fftCount; k++)
                {
                    FFTMat.SetTexture("_ReadBuffer", datas[k][idx1]);
                    Graphics.Blit(null, datas[k][idx], FFTMat, 1);
                }
            }

            return idx;
        }
    }
}