using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace SIF
{
    public struct GpuTileMeshData
    {
        public Vector3[] OriginalPositions;
        public Vector3[] Positions;
        public Vector3[] Normals;
    }
    public class GpuFFTOcean : MonoBehaviour
    {        
        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        int XTileNum = 1;
        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        int ZTileNum = 1;
        /// <summary>
        /// Phillips specturm constant.
        /// </summary>
        [SerializeField]
        float A = 0.000002f;
        /// <summary>
        /// Fourier grid size, should be power of 2.
        /// </summary>
        [SerializeField]
        int N = 64;
        int Np1;
        /// <summary>
        /// Length of each mesh.
        /// </summary>
        [SerializeField]
        [Range(1f, 128f)]
        float L = 32f;

        /// <summary>
        /// Wind direction
        /// </summary>
        [SerializeField]
        Vector2 WindDir = new Vector2(10, 10);

        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        Material OceanMat;

        bool InitFail = false;
        
        /// <summary>
        /// RT for H0
        /// </summary>
        RenderTexture H0Table;
        /// <summary>
        /// RT for H
        /// </summary>
        RenderTexture HTable;
        /// <summary>
        /// RT for dispersion, the W
        /// </summary>
        RenderTexture WTable;
        /// <summary>
        /// 
        /// </summary>
        RenderTexture[] HeightFieldBuffer;
        RenderTexture[] SlopeFieldBuffer;
        RenderTexture[] DisplacementBuffer;
        RenderBuffer[] RenderBuffers012;

        RenderTexture[][] FFTData;

        Material H0TableMat;
        Material WTableMat;
        Material HSDBufferMat;// H for height, S for slope, D for Displacement
        Material DisplacementBufferMat;

        GpuTileMeshData TileData;
        Mesh TileMesh;
        GameObject[] Tiles;

        GpuFFT FFTer;

        void CreateRT(out RenderTexture rt, int width, int height, int depth, RenderTextureFormat format, 
                        RenderTextureReadWrite readWrite, FilterMode filter, TextureWrapMode wrap, bool useMipmaps)
        {
            rt = new RenderTexture(width, height, depth, format, readWrite);
            rt.filterMode = filter;
            rt.wrapMode = wrap;
            rt.autoGenerateMips = useMipmaps;
        }
        private void CreateRenderTextures()
        {
            CreateRT(out H0Table, Np1, Np1, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, FilterMode.Point, TextureWrapMode.Repeat, false);
            CreateRT(out WTable, Np1, Np1, 0, RenderTextureFormat.BGRA32, RenderTextureReadWrite.Linear, FilterMode.Point, TextureWrapMode.Repeat, false);

            HeightFieldBuffer = new RenderTexture[2];
            SlopeFieldBuffer = new RenderTexture[2];
            DisplacementBuffer = new RenderTexture[2];

            for (int i = 0; i < 2; i++)
            {
                CreateRT(out HeightFieldBuffer[i], N, N, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, FilterMode.Point, TextureWrapMode.Clamp, false);
                CreateRT(out SlopeFieldBuffer[i], N, N, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, FilterMode.Point, TextureWrapMode.Clamp, false);
                CreateRT(out DisplacementBuffer[i], N, N, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default, FilterMode.Point, TextureWrapMode.Clamp, false);
            }

            RenderBuffers012 = new RenderBuffer[3]
            {
                HeightFieldBuffer[1].colorBuffer,
                SlopeFieldBuffer[1].colorBuffer,
                DisplacementBuffer[1].colorBuffer
            };
        }

        private void CreateH0Table()
        {
            Graphics.Blit(null, H0Table, H0TableMat);
        }
        private void CreateWTable()
        {
            Graphics.Blit(null, WTable, WTableMat);
        }

        private void InitHSDBuffers(float time)
        {
            HSDBufferMat.SetTexture("_WTable", WTable);
            HSDBufferMat.SetTexture("_H0Table", H0Table);
            HSDBufferMat.SetFloat("_GTime", time);
            Graphics.SetRenderTarget(RenderBuffers012, HeightFieldBuffer[0].depthBuffer);
            Graphics.Blit(null, HSDBufferMat);
        }

        bool CheckMaterials()
        {
            H0TableMat = new Material(Shader.Find("WaterAllInOne/GpuFFTOcean/H0Table"));
            WTableMat = new Material(Shader.Find("WaterAllInOne/GpuFFTOcean/WTable"));
            HSDBufferMat = new Material(Shader.Find("WaterAllInOne/GpuFFTOcean/HSDTable"));

            return true;
        }

        // Use this for initialization
        void Start()
        {
            Np1 = N + 1;
            if (!CheckMaterials())
            {
                InitFail = true;
                return;
            }

            Shader.SetGlobalFloat("N", N);
            Shader.SetGlobalFloat("L", L);
            Shader.SetGlobalFloat("A", A);
            Shader.SetGlobalVector("WindDir", WindDir);

            FFTer = new GpuFFT(N);
            CreateRenderTextures();
            CreateH0Table();
            CreateWTable();

            GenerateOceanVertices();
            TileMesh = GenerateMesh();
            Tiles = GenerateTiles(TileMesh);

            OceanMat.SetFloat("_MeshSize", N);
            OceanMat.SetTexture("_Heightmap", HeightFieldBuffer[1]);
            OceanMat.SetTexture("_Slopemap", SlopeFieldBuffer[1]);
            OceanMat.SetTexture("_Displacementmap", DisplacementBuffer[1]);

            FFTData = new RenderTexture[3][] { HeightFieldBuffer, SlopeFieldBuffer, DisplacementBuffer };
        }
    
        // Update is called once per frame
        void Update()
        {
            if (InitFail)
                return;

            InitHSDBuffers(Time.time*4f);
            FFTer.PerformFFT(0, FFTData);

            TileMesh.vertices = TileData.Positions;
            TileMesh.normals = TileData.Normals;
        }

        private void GenerateOceanVertices()
        {
            TileData = new GpuTileMeshData()
            {
                Positions = new Vector3[Np1 * Np1],
                OriginalPositions = new Vector3[Np1 * Np1],
                Normals = new Vector3[Np1 * Np1],
            };

            for (int n = 0; n < Np1; n++)
            {
                for (int m = 0; m < Np1; m++)
                {
                    int index = n * Np1 + m;
                    TileData.OriginalPositions[index] = new Vector3(m * L / N, 0, n * L / N);
                    TileData.Positions[index] = TileData.OriginalPositions[index];
                }
            }
        }

        Mesh GenerateMesh()
        {
            Vector2[] texcoords = new Vector2[Np1 * Np1];
            int[] indices = new int[(Np1 - 1) * (Np1 - 1) * 6];

            int num = 0;
            for (int y = 0; y < Np1; y++)
            {
                for (int x = 0; x < Np1; x++)
                {
                    int index = x + y * Np1;
                    texcoords[index] = new Vector3((float)x / (Np1 - 1), (float)y / (Np1 - 1));

                    if (x == Np1 - 1 || y == Np1 - 1)
                        continue;
                    // Unity3D use the Left-Hand-Rule and the clockwise rule.Note that the first vertex locate at the bottom-left.
                    indices[num++] = x + y * Np1;
                    indices[num++] = x + (y + 1) * Np1;
                    indices[num++] = (x + 1) + y * Np1;

                    indices[num++] = x + (y + 1) * Np1;
                    indices[num++] = (x + 1) + (y + 1) * Np1;
                    indices[num++] = (x + 1) + y * Np1;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = TileData.Positions;
            mesh.uv = texcoords;
            mesh.triangles = indices;

            return mesh;
        }

        GameObject[] GenerateTiles(Mesh mesh)
        {
            GameObject[] tiles = new GameObject[XTileNum * ZTileNum];
            for (int x = 0; x < XTileNum; x++)
            {
                for (int z = 0; z < ZTileNum; z++)
                {
                    int index = x + z * XTileNum;
                    tiles[index] = new GameObject("Tile" + index.ToString());
                    MeshFilter mf = tiles[index].AddComponent<MeshFilter>();
                    MeshRenderer mr = tiles[index].AddComponent<MeshRenderer>();
                    mr.material = OceanMat;
                    mf.mesh = mesh;
                    tiles[index].transform.parent = transform;
                    tiles[index].transform.localPosition = new Vector3(x * L - XTileNum * L * 0.5f, 0f, z * L - ZTileNum * L * 0.5f);
                }
            }
            return tiles;
        }
    }
}
