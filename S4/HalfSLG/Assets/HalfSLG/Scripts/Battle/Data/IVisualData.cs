
namespace ELGame
{
    public interface IVisualData<D, R> 
        where R : IVisualRenderer<D, R>
        where D : IVisualData<D, R>
    {
        void ConnectRenderer(R renderer);
        void DisconnectRenderer();
    }
}