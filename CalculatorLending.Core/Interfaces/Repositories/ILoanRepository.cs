using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;

namespace CalculatorLending.Core.Interfaces.Repositories;

public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(int id, CancellationToken ct); // tracked, with ItemCopy, for update
    Task<Loan?> GetDetailsByIdAsync(int id, CancellationToken ct); // read-only, with ItemCopy and Item
    Task<int> CountActiveByMemberAsync(int memberId, CancellationToken ct);
    Task<PagedResult<Loan>> GetPagedByMemberAsync(int memberId, LoanQueryParameters q, CancellationToken ct);
    Task<PagedResult<Loan>> GetPagedAsync(LoanQueryParameters q, CancellationToken ct); // admin, with overdue filter
    void Add(Loan loan);
}
