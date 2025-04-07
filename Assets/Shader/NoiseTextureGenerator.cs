using UnityEngine;

public class NoiseTextureGenerator : MonoBehaviour
{
    public enum NoiseType { Perlin, Simplex, Cellular }
    
    [Header("Texture Settings")]
    public NoiseType noiseType = NoiseType.Perlin;
    public int textureSize = 256;
    public float scale = 10f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed = 42;
    
    [Header("Output")]
    public bool saveTextureOnGenerate = false;
    public string saveFileName = "NoiseTexture";
    
    private Texture2D noiseTexture;
    
    [ContextMenu("Generate Noise Texture")]
    public void GenerateNoiseTexture()
    {
        noiseTexture = new Texture2D(textureSize, textureSize);
        
        // Initialiser un générateur de nombres aléatoires avec la graine
        System.Random prng = new System.Random(seed);
        
        // Décalages aléatoires pour chaque octave
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        float maxNoiseValue = float.MinValue;
        float minNoiseValue = float.MaxValue;
        
        float[,] noiseMap = new float[textureSize, textureSize];
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseValue = 0;
                
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x / (float)textureSize * scale * frequency) + octaveOffsets[i].x;
                    float sampleY = (y / (float)textureSize * scale * frequency) + octaveOffsets[i].y;
                    
                    float perlinValue = 0;
                    switch (noiseType)
                    {
                        case NoiseType.Perlin:
                            perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                            break;
                        // Note: Simplex et Cellular nécessitent des implémentations personnalisées
                        // ou des plugins externes comme FastNoise
                        default:
                            perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                            break;
                    }
                    
                    noiseValue += perlinValue * amplitude;
                    
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                
                // Mettre à jour les valeurs min et max
                if (noiseValue > maxNoiseValue) maxNoiseValue = noiseValue;
                if (noiseValue < minNoiseValue) minNoiseValue = noiseValue;
                
                noiseMap[x, y] = noiseValue;
            }
        }
        
        // Normaliser les valeurs entre 0 et 1
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseValue, maxNoiseValue, noiseMap[x, y]);
                
                Color pixelColor = new Color(noiseMap[x, y], noiseMap[x, y], noiseMap[x, y]);
                noiseTexture.SetPixel(x, y, pixelColor);
            }
        }
        
        noiseTexture.Apply();
        
        // Enregistrer la texture si demandé
        if (saveTextureOnGenerate)
        {
            SaveTextureAsPNG();
        }
    }
    
    void SaveTextureAsPNG()
    {
        if (noiseTexture == null) return;
        
        byte[] bytes = noiseTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + saveFileName + ".png", bytes);
        Debug.Log("Noise texture saved to: " + Application.dataPath + "/" + saveFileName + ".png");
    }
    
    public Texture2D GetNoiseTexture()
    {
        if (noiseTexture == null)
            GenerateNoiseTexture();
            
        return noiseTexture;
    }
}