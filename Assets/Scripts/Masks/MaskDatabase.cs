using System.Collections.Generic;

public static class MaskDatabase
{
    private static readonly Dictionary<MaskType, MaskData> Data = new()
    {
        { MaskType.None, new MaskData("No Mask", 0, 0f) },
        { MaskType.Gold, new MaskData("Golden Mask", 100, 0.05f) },
        { MaskType.Blue, new MaskData("Azure Mask", 50, 0.15f) },
        { MaskType.Silver, new MaskData("Silver Mask", 30, 0.30f) },
        { MaskType.Bronze, new MaskData("Bronze Mask", 10, 0.50f) }
    };

    public static MaskData Get(MaskType type) => Data[type];
}