using TaskFlow.Application.Common.Models;

namespace TaskFlow.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}
