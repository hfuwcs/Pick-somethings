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
        // LOG 1: Báo hiệu bắt đầu
        Debug.Log($"[Data Debug] Đang tìm file '{GameUtility.xmlFileName}' trong Resources...");

        // Load file
        TextAsset _xml = Resources.Load<TextAsset>(GameUtility.xmlFileName);

        if (_xml == null)
        {
            // LOG 2: Báo lỗi nếu không thấy file
            Debug.LogError($"[Data Debug] LỖI CỰC MẠNH! Resources.Load trả về NULL. Kiểm tra lại tên file hoặc thư mục Assets/Resources.");
            result = false;
            return new Data();
        }

        // LOG 3: Báo hiệu đã thấy file và độ dài nội dung
        Debug.Log($"[Data Debug] Đã tìm thấy file! Nội dung dài {_xml.text.Length} ký tự.");
        // Debug.Log(_xml.text); // Bỏ comment dòng này nếu muốn in nội dung ra xem (chỉ nên dùng khi text ngắn)

        try
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(Data));
            using (StringReader reader = new StringReader(_xml.text))
            {
                var data = (Data)deserializer.Deserialize(reader);

                // LOG 4: Báo hiệu Deserialize thành công và số lượng câu hỏi đọc được
                Debug.Log($"[Data Debug] Đọc XML thành công! Tổng số câu hỏi: {data.Questions.Length}");

                result = true;
                return data;
            }
        }
        catch (System.Exception ex)
        {
            // LOG 5: Báo lỗi nếu nội dung XML bị sai cú pháp
            Debug.LogError($"[Data Debug] Lỗi khi phân tích XML (Deserialize): {ex.Message}");
            result = false;
            return new Data();
        }
    }
}