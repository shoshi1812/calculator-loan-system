using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CalculatorLending.Data.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _context;

    public LoanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Loans
            .Include(l => l.ItemCopy)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<Loan?> GetDetailsByIdAsync(int id, CancellationToken ct)
    {
        return await _context.Loans
            .AsNoTracking()
            .Include(l => l.ItemCopy)
            .ThenInclude(c => c.Item)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<int> CountActiveByMemberAsync(int memberId, CancellationToken ct)
    {
        return await _context.Loans
            .CountAsync(l => l.MemberId == memberId && l.ReturnDate == null, ct);
    }

    public async Task<PagedResult<Loan>> GetPagedByMemberAsync(int memberId, LoanQueryParameters q, CancellationToken ct)
    {
        var query = _context.Loans
            .AsNoTracking()
            .Where(l => l.MemberId == memberId);

        return await ToPagedAsync(ApplyOverdueFilter(query, q), q, ct);
    }

    public async Task<PagedResult<Loan>> GetPagedAsync(LoanQueryParameters q, CancellationToken ct)
    {
        var query = _context.Loans.AsNoTracking();

        return await ToPagedAsync(ApplyOverdueFilter(query, q), q, ct);
    }

    public void Add(Loan loan)
    {
        _context.Loans.Add(loan);
    }

    private static IQueryable<Loan> ApplyOverdueFilter(IQueryable<Loan> query, LoanQueryParameters q)
    {
        if (q.Overdue == true)
        {
            // Computed outside the query so EF sends it as a parameter
            var now = DateTime.UtcNow;
            query = query.Where(l => l.ReturnDate == null && l.DueDate < now);
        }

        return query;
    }

    private static async Task<PagedResult<Loan>> ToPagedAsync(IQueryable<Loan> query, LoanQueryParameters q, CancellationToken ct)
    {
        var totalCount = await query.CountAsync(ct);

        var loans = await query
            .OrderByDescending(l => l.LoanDate)
            .ThenByDescending(l => l.Id)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Include(l => l.ItemCopy)
            .ThenInclude(c => c.Item)
            .ToListAsync(ct);

        return new PagedResult<Loan>
        {
            Items = loans,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount
        };
    }
}
