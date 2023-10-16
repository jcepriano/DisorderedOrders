using System.Reflection.Metadata.Ecma335;

namespace DisorderedOrdersMVC.Services
{
    public interface IProcessRefund
    {
        public bool ProcessRefund(int amount);
    }
}
