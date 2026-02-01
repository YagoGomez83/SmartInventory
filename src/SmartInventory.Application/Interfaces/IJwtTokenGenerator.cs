using SmartInventory.Domain.Entities;

namespace SmartInventory.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
