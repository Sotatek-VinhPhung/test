namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Password hashing abstraction — implemented in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
