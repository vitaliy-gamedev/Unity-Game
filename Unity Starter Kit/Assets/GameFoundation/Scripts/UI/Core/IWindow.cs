namespace GameFoundation.UI
{
    public interface IWindow
    {
        void Open();
        void Close();
        bool IsOpen { get; }
    }
}
