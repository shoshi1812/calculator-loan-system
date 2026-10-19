using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Repositories;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(int id, CancellationToken ct);
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    void Add(Member member);
}
