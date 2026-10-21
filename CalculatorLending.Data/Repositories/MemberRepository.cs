using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Data.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly AppDbContext _context;

    public MemberRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<Member?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == normalized, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _context.Members.AnyAsync(m => m.Email == normalized, ct);
    }

    public void Add(Member member)
    {
        _context.Members.Add(member);
    }
}
