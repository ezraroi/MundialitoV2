
namespace Mundialito.DAL;


public class ObjectNotFoundException : Exception {

    public ObjectNotFoundException()
    {
    }

    public ObjectNotFoundException(string message)
        : base(message)
    {
    }

}