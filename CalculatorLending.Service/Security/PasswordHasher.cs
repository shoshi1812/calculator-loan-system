using CalculatorLending.Core.Interfaces.Security;

namespace CalculatorLending.Service.Security;

public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    // BCrypt generates a random salt and stores it inside the hash string
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
