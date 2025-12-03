using UnityEngine;
using System.IO;
using System.Xml.Serialization;

public class GameUtility
{
    public const float ResolutionDelayTime = 1;
    public const string SavePrefKey = "Game_HighScore_Value";
    public const string xmlFileName = "Questions_Data";
}

[System.Serializable()]
public class Data
{
    public Question[] Questions = new Question[0];

    public Data() { }

#if UNITY_EDITOR
    public static void Write(Data data)
    {
        string path = Application.dataPath + "/Resources/" + GameUtility.xmlFileName + ".xml";
        XmlSerializer serializer = new XmlSerializer(typeof(Data));
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            serializer.Serialize(stream, data);
        }
        UnityEditor.AssetDatabase.Refresh();
    }
#endif
    public static Data Fetch()
    {
        return Fetch(out bool result);
    }

    public static Data Fetch(out bool result)
    {
        TextAsset _xml = Resources.Load<TextAsset>(GameUtility.xmlFileName);

        if (_xml == null)
        {
            result = false;
            return new Data();
        }
        try
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(Data));
            using (StringReader reader = new StringReader(_xml.text))
            {
                var data = (Data)deserializer.Deserialize(reader);
                result = true;
                return data;
            }
        }
        catch (System.Exception ex)
        {
            result = false;
            return new Data();
        }
    }
}