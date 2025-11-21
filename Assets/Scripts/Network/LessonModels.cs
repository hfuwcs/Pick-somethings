using System;
using UnityEngine;

[Serializable]
public class LessonSummary
{
    public int id;
    public string title;
    public int version;
}

[Serializable]
public class LessonDetail
{
    public int id;
    public string title;
    public int version;
    public string[] pages;
}

public static class JsonHelper
{
    public static T[] GetArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}