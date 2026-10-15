using System.IO;
using UnityEditor;
using UnityEngine;
using BubbleShooter.Bubble;

public static class BubbleSpriteExporter
{
    [MenuItem("Tools/Bubble/Export Glossy Sprites")]
    private static void Export()
    {
        Save(BubbleSpriteFactory.GetBaseSprite().texture, "BubbleBase");
        Save(BubbleSpriteFactory.GetGlossSprite().texture, "BubbleGloss");
        AssetDatabase.Refresh();
        Debug.Log("BubbleBase.png aur BubbleGloss.png Assets folder mein ban gayi hain.");
    }

    private static void Save(Texture2D tex, string name)
    {
        File.WriteAllBytes("Assets/" + name + ".png", tex.EncodeToPNG());
    }
}