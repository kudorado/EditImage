using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Prankard.FlashSpriteSheetImporter
{
    public class FolderSpriteSheetImporterWindow : EditorWindow
    {
        private static Dictionary<SpriteDataFormat, ISpriteSheetParser> spriteParsers =
            new Dictionary<SpriteDataFormat, ISpriteSheetParser>()
            {
                { SpriteDataFormat.StarlingOrSparrowV2, new StarlingParser() }
            };

        [MenuItem("CatzTool/Folder Exporter")]
        static void Init()
        {
            FolderSpriteSheetImporterWindow window =
                (FolderSpriteSheetImporterWindow)EditorWindow.GetWindow(typeof(FolderSpriteSheetImporterWindow));
            window.titleContent = new GUIContent("Sprite Sheet Importer", "Sprite Sheet Importer");
            window.Show();
        }

        private Vector2 customPivot = Vector2.zero;
        private DefaultAsset sourceFolder;
        private string folderPath;
        private List<Texture2D> listTexture = new List<Texture2D>();
        private List<TextAsset> listTextAsset = new List<TextAsset>();

        private SpriteDataFormat dataFormat = SpriteDataFormat.StarlingOrSparrowV2;
        private SpriteAlignment spriteAlignment = SpriteAlignment.Center;
        private bool forcePivotOverwrite = true;
        // private bool generateAnimationController = true;
        // private bool generateGameObject = true;
        // private float fps = 24.0f;

        private DefaultAsset lastDefaultAsset;
        private string errorMessage = null;

   

        void OnGUI()
        {
            GUILayout.Label("Source Folder", EditorStyles.boldLabel);
            sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "Select Folder",
                sourceFolder,
                typeof(DefaultAsset),
                false);
            folderPath = AssetDatabase.GetAssetPath(sourceFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                listTexture.Clear();
                listTextAsset.Clear();
                lastDefaultAsset = null;
                EditorGUILayout.HelpBox(
                    "Select valid folder",
                    MessageType.Warning,
                    true);
            }
            else
            {
                if (lastDefaultAsset == null || sourceFolder != lastDefaultAsset)
                {
                    Debug.Log(daDiretory);
                    RefreshListSprite();
                }
            }

            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Label("Cannot Import", EditorStyles.boldLabel);
                GUILayout.Label("Please fix all sprite size.", EditorStyles.helpBox);

                if (GUILayout.Button("Refresh"))
                {
                    RefreshListSprite();
                }
            }
            else if (listTexture.Count * listTextAsset.Count > 0 && listTexture.Count == listTextAsset.Count)
            {
                if (GUILayout.Button("Import"))
                {
                    errorMessage = null;

                    for (int i = 0; i < listTexture.Count; i++)
                    {
                        Texture2D spriteSheet = listTexture[i];
                        TextAsset textAsset = listTextAsset[i];

                        if (spriteParsers[dataFormat].ParseAsset(spriteSheet, textAsset,
                                forcePivotOverwrite ? PivotValue : new Vector2(0f, 1.0f), forcePivotOverwrite))
                        {
                            SplitSpriteSheet(i);
                            continue;
                        }

                        Debug.LogError("Failed To Parse Asset: " + textAsset.name);
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                GUILayout.Label("Cannot Import", EditorStyles.boldLabel);
                GUILayout.Label("Please select a folder sprite sheet and text asset to import sprite sheet",
                    EditorStyles.helpBox);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
            }
        }

        public void RefreshListSprite()
        {
            errorMessage = "";
            listTexture.Clear();
            listTextAsset.Clear();

            // daModFolder = sourceFolder.name;

            lastDefaultAsset = sourceFolder;
            var folders = new string[] { folderPath };
            var guids = AssetDatabase.FindAssets("t:Texture2D", folders);

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var tex = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                listTexture.Add((Texture2D)tex);

                Vector2 size = GetOriginalSize(listTexture[i]);
                if (size.x != listTexture[i].width && size.y != listTexture[i].height)
                {
                    if (string.IsNullOrEmpty(errorMessage)) errorMessage = "Check size:\n";
                    errorMessage += Path.GetFileNameWithoutExtension(path) + ":   " + Math.Max(size.x, size.y) + "\n";
                }

                foreach (ISpriteSheetParser parser in spriteParsers.Values)
                {
                    var dataAssetPath = Path.GetDirectoryName(path) + "/" +
                                        Path.GetFileNameWithoutExtension(path) + "." +
                                        parser.FileExtension;
                    TextAsset searchTextAsset =
                        AssetDatabase.LoadAssetAtPath(dataAssetPath, typeof(TextAsset)) as TextAsset;
                    if (searchTextAsset != null)
                    {
                        listTextAsset.Add(searchTextAsset);
                        break;
                    }
                }
            }
        }

        public static Vector2 GetOriginalSize(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return new Vector2(texture.width, texture.height);

            object[] array = new object[] { 0, 0 };
            MethodInfo mi =
                typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Invoke(importer, array);

            return new Vector2((int)array[0], (int)array[1]);
        }

        public Vector2 PivotValue
        {
            get
            {
                switch (spriteAlignment)
                {
                    case SpriteAlignment.TopLeft:
                        return new Vector2(0f, 1f);
                    case SpriteAlignment.TopCenter:
                        return new Vector2(0.5f, 1f);
                    case SpriteAlignment.TopRight:
                        return new Vector2(1f, 1f);
                    case SpriteAlignment.LeftCenter:
                        return new Vector2(0f, 0.5f);
                    case SpriteAlignment.Center:
                        return new Vector2(0.5f, 0.5f);
                    case SpriteAlignment.RightCenter:
                        return new Vector2(1f, 0.5f);
                    case SpriteAlignment.BottomLeft:
                        return new Vector2(0f, 0f);
                    case SpriteAlignment.BottomCenter:
                        return new Vector2(0.5f, 0f);
                    case SpriteAlignment.BottomRight:
                        return new Vector2(1f, 0f);
                    case SpriteAlignment.Custom:
                        return customPivot;
                    default:
                        throw new System.NotImplementedException("I don't know the sprite alignment: " +
                                                                 spriteAlignment.ToString());
                }
            }
        }


        #region spriteImporterWizard

        private class SubTexture
        {
            public int width;
            public int height;
            public int x;
            public int y;
            public string name;
            public int frameX { get; set; }
            public int frameY { get; set; }
            public int frameWidth { get; set; }
            public int frameHeight { get; set; }

            public void SetFrame(int x, int y, int w, int h)
            {
                frameX = x;
                frameY = y;
                frameWidth = w;
                frameHeight = h;
            }

            public string GetString()
            {
                return string.Format("{0} w: {1} h: {2} frameX: {3} frameY: {4} frameWidth: {5} frameHeight {6}",
                    name, width, height, frameX, frameY, frameWidth, frameHeight);
            }
        }

        private SubTexture[] subTextures;
        private Direction offset = Direction.None;
        private string daDiretory = Directory.GetCurrentDirectory() + "/SpriteExport/";

        private void SplitSpriteSheet(int ii)
        {
            ParseXML(ii);
            var spriteSheet = listTexture[ii];
            var path = AssetDatabase.GetAssetPath(spriteSheet);
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);

            var n = Path.GetFileNameWithoutExtension(path);

            float totals = objs.Length;

            int maxWidth = 0;
            int maxHeight = 0;

            var sprites1 = objs.Where(q => q is Sprite).Cast<Sprite>();

            foreach (var s in sprites1)
            {
                var w = (int)s.textureRect.width;
                var h = (int)s.textureRect.height;

                maxWidth = w > maxWidth ? w : maxWidth;
                maxHeight = h > maxHeight ? h : maxHeight;
            }

            maxWidth = 0;
            maxHeight = 0;
            for (int i = 0; i < objs.Length; i++)
            {
                var s = objs[i] as Sprite;

                if (s != null)
                {
                    // this is a sprite object

                    var rect = s.rect;
                    int w = (int)rect.width;
                    int h = (int)rect.height;
                    maxWidth = w > maxWidth ? w : maxWidth;
                    maxHeight = h > maxHeight ? h : maxHeight;
                }
            }

            System.IO.Directory.CreateDirectory(daDiretory + n);
            var readableTex = duplicateTexture(spriteSheet);
            for (int i = 0; i < objs.Length; i++)
            {
                var s = objs[i] as Sprite;

                if (s != null)
                {
                    var rect = s.rect;
                    var pixels = readableTex.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y),
                        Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));

                    int width = Mathf.RoundToInt(s.rect.width); //.round();
                    int height = Mathf.RoundToInt(s.rect.height); //.round();
                    string outputName = daDiretory + n + "/" +
                                        (char.ToUpper(objs[i].name[0]) + objs[i].name.Substring(1)) +
                                        ".png";

                    EditorUtility.DisplayProgressBar("Progressing asset", outputName, i / totals);

                    if (offset != Direction.None)
                        CreateSpriteBoundFix(outputName, width, maxHeight, pixels, width, height);
                    else
                        CreateSprite1(outputName, width, height, pixels, GetSubTexture(s.name), s);
                }
            }
        }

        private void ParseXML(int i)
        {
            try
            {
                var document = new XmlDocument();
                document.LoadXml(listTextAsset[i].text);

                var root = document.DocumentElement;
                if (root == null || root.Name != "TextureAtlas")
                {
                    return;
                }

                subTextures = null;
                subTextures = root.ChildNodes
                    .Cast<XmlNode>()
                    .Where(childNode => childNode.Name == "SubTexture")
                    .Select(childNode => new SubTexture
                    {
                        width = Convert.ToInt32(childNode.Attributes["width"].Value),
                        height = Convert.ToInt32(childNode.Attributes["height"].Value),
                        x = Convert.ToInt32(childNode.Attributes["x"].Value),
                        y = Convert.ToInt32(childNode.Attributes["y"].Value),
                        name = childNode.Attributes["name"].Value
                    }).ToArray();

                var listNodes = root.ChildNodes
                    .Cast<XmlNode>()
                    .Where(childNode => childNode.Name == "SubTexture");
                foreach (var item in subTextures)
                {
                    string name = item.name;
                    int frameX = 0;
                    int frameY = 0;
                    int frameWidth = item.width;
                    int frameHeight = item.height;
                    foreach (var item1 in listNodes)
                    {
                        if (item1.Attributes["name"].Value == name)
                        {
                            if (item1.Attributes["frameX"] != null
                                && item1.Attributes["frameY"] != null
                                && item1.Attributes["frameWidth"] != null
                                && item1.Attributes["frameHeight"] != null)
                            {
                                frameX = Convert.ToInt32(item1.Attributes["frameX"].Value);
                                frameY = Convert.ToInt32(item1.Attributes["frameY"].Value);
                                frameWidth = Convert.ToInt32(item1.Attributes["frameWidth"].Value);
                                frameHeight = Convert.ToInt32(item1.Attributes["frameHeight"].Value);
                            }
                        }
                    }

                    item.SetFrame(frameX, frameY, frameWidth, frameHeight);
                }
            }
            catch (Exception)
            {
                subTextures = null;
            }
        }

        Texture2D duplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        private SubTexture GetSubTexture(string name)
        {
            if (subTextures != null)
            {
                foreach (var item in subTextures)
                {
                    if (item.name == name)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        void CreateSprite(string path, int width, int height, Color[] colors, SubTexture _subTexture)
        {
            Texture2D tex2D = new Texture2D(width, height);
            Color[] outputColors = new Color[width * height];

            int i = 0;
            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < height; x++)
                {
                    outputColors[i] = colors[i];
                    i++;
                }
            }

            tex2D.SetPixels(colors);
            tex2D.Apply();
            var bytes = tex2D.EncodeToPNG();

            Thread thread = new Thread(() => { System.IO.File.WriteAllBytes(path, bytes); });

            thread.Start();
            thread.Join();

            AssetDatabase.Refresh();
        }

        void CreateSprite1(string path, int width, int height, Color[] colors, SubTexture _subTexture, Sprite s)
        {
            if (_subTexture == null)
            {
                CreateSprite(path, width, height, colors, _subTexture);
            }
            else
            {
                var texShit = new Texture2D(width, height);
                texShit.SetPixels(colors);
                texShit.Apply();

                int startX = 0;
                int startY = 0;


                startX = (_subTexture.frameWidth / 2) - (_subTexture.width / 2) + _subTexture.frameX;
                startY = (_subTexture.frameHeight / 2) - (_subTexture.height / 2) + _subTexture.frameY;

                startX = (int)((_subTexture.frameWidth * 0.5f) - (s.pivot.x));
                startY = (int)((_subTexture.frameHeight * 0.5f) - (s.pivot.y));

                Texture2D tex2D = new Texture2D(_subTexture.frameWidth, _subTexture.frameHeight);
                int w = _subTexture.frameWidth;
                int h = _subTexture.frameHeight;

                int y = 0;
                while (y < h)
                {
                    int x = 0;
                    while (x < w)
                    {
                        tex2D.SetPixel(x, y, new Color(0, 0, 0, 0));
                        if (x >= startX && x < startX + width && y >= startY && y < startY + height)
                        {
                            tex2D.SetPixel(x, y, texShit.GetPixel(x - startX, y - startY));
                        }

                        ++x;
                    }

                    ++y;
                }

                tex2D.Apply();
                var bytes = tex2D.EncodeToPNG();

                Thread thread = new Thread(() => { System.IO.File.WriteAllBytes(path, bytes); });

                thread.Start();
                thread.Join();

                AssetDatabase.Refresh();
            }
        }

        void CreateSpriteBoundFix(string path, int w, int h, Color[] colors, int realWidth, int realHeight)
        {
            var texShit = new Texture2D(realWidth, realHeight);
            texShit.SetPixels(colors);
            texShit.Apply();


            int width = realWidth;
            int height = realHeight;

            int startX = 0;
            int startY = 0;


            switch (offset)
            {
                case Direction.BottomRight:
                    startX = w - width;
                    startY = h - height;
                    break;

                case Direction.BottomLeft:
                    startX = 0;
                    startY = h - height;
                    break;

                case Direction.BottomCenter:
                    startX = (w / 2) - (width / 2);
                    startY = h - height;
                    break;
            }

            // create new texture
            Texture2D tex2D = new Texture2D(w, h);
            // right corner
            int y = 0;
            while (y < h)
            {
                int x = 0;
                while (x < w)
                {
                    // set normal pixels
                    tex2D.SetPixel(x, y, new Color(0, 0, 0, 0));
                    // if we are at bottom right apply logo 
                    //TODO also check alpha, if there is no alpha apply it!

                    switch (offset)
                    {
                        case Direction.BottomRight:
                            if (x >= startX && y < h - startY)
                                tex2D.SetPixel(x, y, texShit.GetPixel(x - startX, y));

                            break;

                        case Direction.BottomLeft:
                            if (x >= startX && x < startX + width && y < h - startY)
                                tex2D.SetPixel(x, y, texShit.GetPixel(x - startX, y));

                            break;


                        case Direction.BottomCenter:
                            if (x >= startX && x < startX + width && y < h - startY)
                                tex2D.SetPixel(x, y, texShit.GetPixel(x - startX, y));

                            break;
                    }


                    ++x;
                }

                ++y;
            }

            tex2D.Apply();
            var bytes = tex2D.EncodeToPNG();

            Thread thread = new Thread(() => { System.IO.File.WriteAllBytes(path, bytes); });

            thread.Start();
            thread.Join();

            AssetDatabase.Refresh();
        }

        #endregion
    }

    [System.Serializable]
    public enum Direction
    {
        None,
        BottomRight,
        BottomLeft,
        BottomCenter,
    }
}