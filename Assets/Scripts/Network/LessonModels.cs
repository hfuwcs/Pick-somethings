using System;
using System.Collections.Generic;

[Serializable]
public class LessonResponse
{
    public int id;
    public string title;
    public int version;
    public string[] pages;
}