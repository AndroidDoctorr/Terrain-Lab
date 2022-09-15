using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainBuilder : MonoBehaviour
{
    private static Dictionary<string, BlockData> _terrainData = new Dictionary<string, BlockData>();

    public int RenderLimit = 63;

    public float BlockSize = 0.1f;
    public float BaseHeight;
    public float StartZ;
    public float ZScale;
    public int MountainStart = 90;
    public int HillsStart = 60;
    public int ValleyStart = 20;
    public int WaterLevel = 5;

    public float BaseX;
    public float BaseY;
    public float TerrainGrain;

    public float WoodBaseX;
    public float WoodBaseY;
    public float WoodGrain;
    public float WoodFrequency;
    public float WoodDensity;
    public float WoodMaxZ = 7;
    private float WoodVariance = 0.35f;

    public GameObject TPPlane;
    public GameObject Mountain1Block;
    public GameObject Mountain2Block;
    public GameObject Terrain1Block;
    public GameObject Terrain2Block;
    public GameObject Terrain3Block;
    public GameObject WaterBlock;
    public GameObject[] Trees;

    private void OnEnable()
    {
        GenerateTerrain();
    }

    public static float GetAltCoord(int x, int y)
    {
        // Determine altitude at particular coordinates
        string key = GetCoordString(x, y);
        if (!_terrainData.ContainsKey(key))
            return -1;

        return _terrainData[key].Z;
    }

    public static bool HasTree(int x, int y)
    {
        // Determine whether block at coordinates contains a tree
        string key = GetCoordString(x, y);
        if (!_terrainData.ContainsKey(key))
            return false;
        
        return _terrainData[key].IsWooded;
    }

    private void GenerateTerrain()
    {
        for (int i = -RenderLimit; i < RenderLimit; i++)
        {
            for (int j = -RenderLimit; j < RenderLimit; j++)
            {
                GenerateTerrainBlock(i, j);
            }
        }
    }

    private void GenerateTerrainBlock(int i, int j)
    {
        // Determine block type, slope, and elevation based on Perlin number x 100
        float num = GetPerlinNumber(i, j);
        if (num >= MountainStart)
        {
            // Mountains (z = 9.5 - 13.5)
            float z = GetZFromPerlinNum(num, 90, 100, 1, 4, 9.5f);
            PlaceBlockAt(z > 12 ? Mountain2Block : Mountain1Block, i, j, z);
        }
        else if (num >= HillsStart)
        {
            // Hills (z = 4 - 9.5)
            float z = GetZFromPerlinNum(num, 60, 90, 0.5f, 10, 4);
            GameObject block = Terrain3Block;
            PlaceBlockAt(block, i, j, z);
        }
        else if (num >= ValleyStart)
        {
            // Plains (z = 1 - 4)
            float z = GetZFromPerlinNum(num, 20, 60, 0.5f, 5, 1.5f);
            GameObject block = Terrain2Block;
            PlaceBlockAt(block, i, j, z);
        }
        else if (num > WaterLevel)
        {
            // Valley (z = 1)
            GameObject block = Terrain1Block;
            PlaceBlockAt(block, i, j, 1);
        }
        else
        {
            // River (z = 0.5)
            GameObject block = WaterBlock;
            PlaceBlockAt(block, i, j, 0.5f);
        }
    }

    private float GetZFromPerlinNum(float num, float bottom, float top, float interval, float zInterval, float elevMin)
    {
        float range = top - bottom;
        float deltaZ = num - bottom;
        float z = elevMin + interval * Mathf.Floor(zInterval * deltaZ / range);
        return z;
    }

    private void PlaceBlockAt(GameObject block, int x, int y, float z)
    {
        // Place a terrain block - add extras if necessary
        InstantiateBlock(block, x, y, z);

        bool isWooded = GetWoodedPerlin(x, y, z);

        SaveBlockData(x, y, z, block.name, isWooded);

        AddBlockExtras(x, y, z, isWooded);
    }
    private void SaveBlockData(int x, int y, float z, string name, bool isWooded)
    {
        // Add a BlockData item to the dictionary
        BlockData data = new BlockData()
        {
            Name = name,
            Z = z,
            IsWooded = isWooded,
        };
        _terrainData.Add(GetCoordString(x, y), data);
    }
    private void AddBlockExtras(int x, int y, float z, bool isWooded)
    {
        // Place a teleportation plane or a tree, if necessary
        // Don't place either on water or mountains
        if (z < 1 || z > 9) return;
        // Only place TP planes near the middle
        float halfRange = RenderLimit / 2;
        bool inXBounds = x > -halfRange && x < halfRange;
        bool inYBounds = y > -halfRange && y < halfRange;
        if (inXBounds && inYBounds)
            InstantiateBlock(TPPlane, x, y, z + 1);
        // Only place a tree if the spot is wooded
        if (isWooded)
        {
            int treeType = Random.Range(0, Trees.Length);
            InstantiateBlock(Trees[treeType], x, y, z + 1, true);
        }
    }

    private void InstantiateBlock(GameObject block, int x, int y, float z)
    {
        InstantiateBlock(block, x, y, z, false);
    }

    private void InstantiateBlock(GameObject block, int xp, int yp, float zp, bool isTree)
    {
        // Place terrain, tree, or special block
        float variation = BlockSize * WoodVariance;
        float x = xp * BlockSize + (isTree ? Random.Range(-variation, variation) : 0);
        float y = zp * BlockSize + BaseHeight;
        float z = yp * BlockSize + (isTree ? Random.Range(-variation, variation) : 0);

        Instantiate(block, new Vector3(x, y, z), new Quaternion());
    }
    private int GetPerlinNumber(float i, float j)
    {
        // Get the terrain Perlin number (used to determine altitude)
        float scale = ZScale == 0 ? 1 : ZScale;
        float x = BaseX + TerrainGrain * i;
        float y = BaseY + TerrainGrain * j;
        float z = Mathf.PerlinNoise(x, y) * scale + StartZ;
        int zInt = Mathf.FloorToInt(z * 100);
        return zInt;
    }
    private bool GetWoodedPerlin(int i, int j, float alt)
    {
        // Determine whether or not the block is wooded
        if (alt > WoodMaxZ) return false;
        // Calculate the wooded perlin number (independent of terrain)
        float x = WoodBaseX + WoodGrain * i;
        float y = WoodBaseY + WoodGrain * j;
        float pn = Mathf.PerlinNoise(x, y);
        // Limit to bounds defined by WoodFrequency property
        if (pn > WoodFrequency) return false;
        // Distribute trees randomly - denser at lower elevations
        float range = WoodMaxZ - WoodFrequency;
        float p = 1 - ((alt - WoodFrequency) / range);
        float chance = Random.Range(0f, 1f);

        return chance < (p * WoodDensity);
    }
    private static string GetCoordString(int x, int y)
    {
        return $"{x},{y}";
    }
}
