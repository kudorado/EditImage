// Creates a simple wizard that lets you create a Light GameObject
// or if the user clicks in "Apply", it will set the color of the currently
// object selected to red
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
// using KR;


[System.Serializable]
public enum Direction
{
    None,
    BottomRight,
    BottomLeft,
    BottomCenter,
}
public class SpriteImporterWizard : ScriptableWizard
{

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

    //public float range = 500;
    //public Color color = Color.red;
    public List<Sprite> sprites;
    public Direction offset;

    public string daDiretory = "Assets/Export/";

    [SerializeField]
    private List<TextAsset> xmlAsset;


    [MenuItem("CatzTool/Sprite Exporter")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<SpriteImporterWizard>("Shit fuck you", "Convert");
        //If you don't want to use the secondary button simply leave it out:
        //ScriptableWizard.DisplayWizard<WizardCreateLight>("Create Light", "Create");
    }

    private void ParseXML(int i)
    {
        try
        {
            var document = new XmlDocument();
            document.LoadXml(xmlAsset[i].text);

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
            foreach(var item in subTextures)
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
            foreach(var item in subTextures)
            {
                //Debug.LogError(item.GetString());
            }
        }
        catch (Exception)
        {
            //Debug.LogError("0000");
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


    void OnWizardCreate()
    {
        //GameObject go = new GameObject("New Light");
        //Light lt = go.AddComponent<Light>();
        //lt.range = range;
        //lt.color = color;

        EditorUtility.DisplayProgressBar("Creating Multiple Sprite", "Extracting sprite", 0);


        
        for(int ii= 0; ii<sprites.Count; ii++)
        //foreach (var spriteSheet in sprites)
        {
                ParseXML(ii);
                var spriteSheet = sprites[ii];
                var path = AssetDatabase.GetAssetPath(spriteSheet);
            var objs = AssetDatabase.LoadAllAssetsAtPath(path);

            //Sprite[] sprites = Resources.LoadAll<Sprite>(spriteSheet.name);

            var n = Path.GetFileNameWithoutExtension(path);

            float totals = objs.Length;

            int maxWidth = 0;
            int maxHeight = 0;

            var sprites1 = objs.Where(q => q is Sprite).Cast<Sprite>();

            Debug.Log(sprites1.ToList().Count);

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
                { // this is a sprite object

                    var rect = s.rect;
                    int w = (int)rect.width;
                    int h = (int)rect.height;
                    maxWidth = w > maxWidth ? w : maxWidth;
                    maxHeight = h > maxHeight ? h : maxHeight;
                }
            }

            System.IO.Directory.CreateDirectory(daDiretory + n);
            //var readableTex = spriteSheet.texture;
            var readableTex = duplicateTexture(spriteSheet.texture);
            for (int i = 0; i < objs.Length; i++)
            {
                var s = objs[i] as Sprite;

                //var s1 = GetSubTexture(s.name);
                if (s != null)
                { // this is a sprite object
                    
                    var rect = s.rect;

                    if(rect.width + rect.height <= 0)
                        continue;

                    //var readableTex = s.texture;
                    //var readableTex = duplicateTexture(s.texture);
                    //string xxx = s.name + " pos: "+  s.rect.x + " " + s.rect.y + " size: " + s.rect.size + " pivot: " + s.pivot.ToString();
                    //Debug.LogError(xxx);
                    var pixels = readableTex.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));

                    int width = Mathf.RoundToInt(s.rect.width); //.round();
                    int height = Mathf.RoundToInt(s.rect.height);//.round();
                    string outputName = daDiretory + n + "/" + (char.ToUpper(objs[i].name[0]) + objs[i].name.Substring(1)) + ".png";//path.Replace(n + ".png", (char.ToUpper(objs[i].name[0])  + objs[i].name.Substring(1)) + ".png");  
                                                                                                                                    // outputName = outputName.Replace("Assets/", "Assets/" + n + "/");

                    Debug.Log(outputName + " --- size: " + s.rect.width + " - " + s.rect.height);
                    Debug.Log(objs[i].name);
                    EditorUtility.DisplayProgressBar("Progressing asset", outputName, i / totals);
                    // CreateSprite(outputName , width, height, pixels);

                    if (offset != Direction.None)
                        CreateSpriteBoundFix(outputName, width, maxHeight, pixels, width, height);
                    else
                        CreateSprite1(outputName, width, height, pixels, GetSubTexture(s.name), s);
                }
            }

        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

    }

    private SubTexture GetSubTexture(string name)
    {
        if (subTextures != null)
        {
            foreach (var item in subTextures)
            {
                if(item.name == name)
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

        Thread thread = new Thread(() =>
        {
            System.IO.File.WriteAllBytes(path, bytes);

        });

        thread.Start();
        thread.Join();

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
            Debug.LogError(startX + "   " + startY + "  pivot " + s.pivot.ToString());
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

            Thread thread = new Thread(() =>
            {
                System.IO.File.WriteAllBytes(path, bytes);

            });

            thread.Start();
            thread.Join();

            // AssetDatabase.Refresh();
        }
    }

    void OnWizardUpdate()
    {
        helpString = "Input sprite sheet image.";
    }

    // When the user presses the "Apply" button OnWizardOtherButton is called.
    void OnWizardOtherButton()
    {
        //if (Selection.activeTransform != null)
        //{
        //    Light lt = Selection.activeTransform.GetComponent<Light>();

        //    if (lt != null)
        //    {
        //        lt.color = Color.red;
        //    }
        //}
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

        Thread thread = new Thread(() =>
        {
            System.IO.File.WriteAllBytes(path, bytes);

        });

        thread.Start();
        thread.Join();

        // AssetDatabase.Refresh();
    }
}