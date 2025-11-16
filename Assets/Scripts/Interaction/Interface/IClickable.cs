public interface IClickable
{

    Grabbable AssociatedGrabbable { get; }

    void OnClick();
}