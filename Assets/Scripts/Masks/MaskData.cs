public class MaskData
{
    public string Name { get; }
    public int Points { get; }
    public float SpawnRate { get; }
    

    public MaskData(string name, int points, float spawnRate)
    {
        Name = name;
        Points = points;
        SpawnRate = spawnRate;
    }
}