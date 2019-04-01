using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        //seed is used to get the same world every time if we want to
        System.Random prng = new System.Random(seed);
        //used to sample different octaves from different places
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            //we dont wanna use numbers that are too big, otherwise the Perlin
            //function will just return the same values for some wierd reason
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        //Used to multiply into the x position and y positions
        //so that they are not passed as integers to the
        //PerlinNoise method calls
        if(scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;


        for(int y=0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    //the higher the frequency, the farther apart the sample points will be, which means the height values will change more rapidly
                    //we add in the octave offsets so that each octave samples points from a different area, but each octaves' points are in the same area as other
                    //points in that same octave
                    //We subtract halfWidth and halfHeigh from x and y respectively so that when we change the noise scale, the map "zooms" into the center instead of the top right corner
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

                    //multiply perlinvalue by 2 and subtract 1, so that we can sometimes get negative perlinvalues and have more interesting noise
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    //this is because as we iterate through the octaves, we keep adding on to the result of the last octave
                    //and we do this for each "cell" in the grid for which we are generating terrain
                    noiseHeight += perlinValue * amplitude;

                    //amplitude decreases with each octave
                    amplitude *= persistence;
                    //frequency increases with each octave
                    frequency *= lacunarity;
                }

                //we keep track of the min and max so that we can know the range that our noise values are in
                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                } else if(noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                //now we have calculated the noiseheight for this point, taking into account all the octaves
                noiseMap[x, y] = noiseHeight;
            }
        }

        //this loop goes through the noiseMap and normalizes by making all values be between zero and one, relative to each other
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                //inverseLerp returns a value between zero and one
                //if noiseMap value is equal to maxNoiseHeight, then it returns 1, min returns 0, etc
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}
