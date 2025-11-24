public interface IInfoDisplayable
{
    public struct TooltipInfo
    {
        public string title;
        public string content;

        public TooltipInfo(string title, string content)
        {
            this.title = title;
            this.content = content;
        }
    }

    TooltipInfo GetTooltipInfo();
}
