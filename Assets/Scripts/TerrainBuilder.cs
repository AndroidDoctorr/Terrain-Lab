using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainBuilder : MonoBehaviour
{
    private static Dictionary<string, BlockData> _terrainData = new Dictionary<string, BlockData>();

    public int Range;

    public float BaseHeight;
    public float StartZ;
    public float ZScale;

    public float BaseX;
    public float BaseY;
    public float TerrainGrain;

    public float WoodBaseX;
    public float WoodBaseY;
    public float WoodGrain;
    public float WoodFrequency;
    public float WoodDensity;
    public float WoodMaxZ = 7;

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
        string key = GetCoordString(x, y);
        if (!_terrainData.ContainsKey(key))
        {
            return -1;
        }

        return _terrainData[key].Z;
    }

    public static bool HasTree(int x, int y)
    {
        string key = GetCoordString(x, y);
        if (!_terrainData.ContainsKey(key))
        {
            return false;
        }

        return _terrainData[key].IsWooded;
    }

    private void GenerateTerrain()
    {
        for (int i = -Range; i < Range; i++)
        {
            for (int j = -Range; j < Range; j++)
            {
                GenerateTerrainBlock(i, j);
            }
        }
    }

    private void GenerateTerrainBlock(int i, int j)
    {
        float num = GetPerlinNumber(i, j);

        if (num >= 90)
        {
            // Mountains (6 - 11)
            float z = GetZFromPerlinNum(num, 60, 90, 1, 10, -0.5f);
            PlaceBlockAt(z > 12 ? Mountain2Block : Mountain1Block, i, j, z);
        }
        else if (num >= 60)
        {
            // Hills (3 - 6)
            float z = GetZFromPerlinNum(num, 60, 90, 0.5f, 10, 4);
            GameObject block = Terrain3Block;
            PlaceBlockAt(block, i, j, z);
        }
        else if (num >= 20)
        {
            // Plains (1 - 3)
            float z = GetZFromPerlinNum(num, 20, 60, 0.5f, 5, 1.5f);
            GameObject block = Terrain2Block;
            PlaceBlockAt(block, i, j, z);
        }
        else if (num > 5)
        {
            // Valley (1)
            GameObject block = Terrain1Block;
            PlaceBlockAt(block, i, j, 1);
        }
        else
        {
            // River (0)
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
        InstantiateBlock(block, x, y, z);

        bool isWooded = GetWoodedPerlin(x, y, z);

        BlockData b = new BlockData()
        {
            Name = block.name,
            Z = z,
            IsWooded = isWooded,
        };
        _terrainData.Add(GetCoordString(x, y), b);

        if (z > 0.5 && z < 9)
        {
            bool inXBounds = x > -32 && x < 32;
            bool inYBounds = y > -32 && y < 32;
            bool isInTPBounds = inXBounds && inYBounds;

            if (isInTPBounds)
            {
                InstantiateBlock(TPPlane, x, y, z + 1);
            }

            if (isWooded)
            {
                int treeType = Random.Range(0, Trees.Length);
                InstantiateBlock(Trees[treeType], x, y, z + 1, true);
            }
        }
    }

    private void InstantiateBlock(GameObject block, int x, int y, float z)
    {
        InstantiateBlock(block, x, y, z, false);
    }

    private void InstantiateBlock(GameObject block, int xp, int yp, float zp, bool isTree)
    {
        float x = xp * 0.1f + (isTree ? Random.Range(-0.03f, 0.03f) : 0);
        float y = zp * 0.1f + BaseHeight;
        float z = yp * 0.1f + (isTree ? Random.Range(-0.03f, 0.03f) : 0);

        Instantiate(block, new Vector3(x,y,z), new Quaternion());
    }

    private int GetPerlinNumber(float i, float j)
    {
        float scale = ZScale == 0 ? 1 : ZScale;
        float x = BaseX + TerrainGrain * i;
        float y = BaseY + TerrainGrain * j;
        float z = Mathf.PerlinNoise(x, y) * scale + StartZ;
        int zInt = Mathf.FloorToInt(z * 100);
        return zInt;
    }
    private bool GetWoodedPerlin(int i, int j, float alt)
    {
        float x = WoodBaseX + WoodGrain * i;
        float y = WoodBaseY + WoodGrain * j;
        float z = Mathf.PerlinNoise(x, y);

        if (z > WoodFrequency) return false;
        // return true;

        float range = (WoodMaxZ - WoodFrequency) / 2;
        float middle = WoodFrequency + range;
        float p = 1 - (Mathf.Abs(middle - alt) / range);
        float chance = Random.Range(0f, 1f);

        return chance < (p * WoodDensity);
    }
    private static string GetCoordString(int x, int y)
    {
        return $"{x},{y}";
    }
}
