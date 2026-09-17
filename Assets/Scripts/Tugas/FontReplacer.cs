using UnityEngine;
using UnityEditor;
using TMPro;

public class FontReplacer : EditorWindow
{
    public TMP_FontAsset oldFont;
    public TMP_FontAsset newFont;

    [MenuItem("Utilities/Replace All Fonts")]
    public static void ShowWindow()
    {
        GetWindow<FontReplacer>("Font Replacer");
    }

    void OnGUI()
    {
        oldFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Old Font", oldFont, typeof(TMP_FontAsset), false);
        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", newFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace Fonts in Open Scenes"))
        {
            if (oldFont == null || newFont == null) return;

            // Find all TextMeshPro components in the active scene
            TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            int count = 0;

            foreach (var text in texts)
            {
                if (text.font == oldFont)
                {
                    text.font = newFont;
                    EditorUtility.SetDirty(text);
                    count++;
                }
            }
            Debug.Log($"Successfully replaced font in {count} text objects!");
        }
    }
}
