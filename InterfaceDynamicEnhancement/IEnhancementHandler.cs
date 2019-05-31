using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace InterfaceDynamicEnhancement
{
    public interface IEnhancementHandler<T>
    {
        Task<T2> EnhancementObjectAsync<T2>(T coreObject, bool singleton) where T2 : T;
    }
}
