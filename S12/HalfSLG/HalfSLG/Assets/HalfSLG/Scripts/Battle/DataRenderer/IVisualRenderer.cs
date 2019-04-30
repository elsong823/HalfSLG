
namespace ELGame
{
    public interface IVisualRenderer<D, R>
        where R : IVisualRenderer<D, R>
        where D : IVisualData<D, R>
    {
        void OnConnect(D data);
        void OnDisconnect();
        void RefreshRenderer();
    }
}