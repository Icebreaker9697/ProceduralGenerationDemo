using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        //these two variables are used to center the mesh on the texture
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2 ;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < height; y+= meshSimplificationIncrement)
        {
            for(int x = 0; x < width; x+= meshSimplificationIncrement)
            {
                //add topLeftX to x and subtract y from topLeftZ so that the mesh is centered
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                //ignore right and bottom edge vertices of the map, because there aren't mesh triangles extending from those sides (they would extend out of bounds)
                if(x < width - 1 && y < height - 1)
                {
                    //For any point where we are getting its 2 triangles, the vertices are located at
                    // where w is the width, and i+w means getting index i in the next row (since we are working in a 1d array)
                    // i, i+w+1, i+w; i+w+1, i, i+1;
                    //
                    //
                    //  i  XXXXXXX i+1
                    //  XX   X   X
                    //  X X   X  X
                    //  X  X   X X
                    //  X   X   XX
                    //  XXXXXX   X
                    //  i + w   i + w + 1

                    meshData.AddTriange(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriange(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1)*(meshHeight - 1)*6];
    }

    public void AddTriange(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
