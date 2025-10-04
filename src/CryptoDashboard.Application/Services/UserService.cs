using CryptoDashboard.Domain.Entities;
using CryptoDashboard.Dto.User;

namespace CryptoDashboard.Application.Services
{
    public class UserService
    {
        public UserDto GetUserById(Guid id)
        {
            // Simulação: normalmente buscaria no banco!
            return new UserDto { Id = id, Name = "Usuário Teste" };
        }
    }
}