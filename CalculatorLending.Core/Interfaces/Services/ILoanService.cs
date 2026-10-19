using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;

namespace CalculatorLending.Core.Interfaces.Services;

public interface ILoanService
{
    Task<ServiceResult<LoanResponse>> CreateAsync(int memberId, LoanCreateRequest r, CancellationToken ct);
    Task<ServiceResult<LoanResponse>> ReturnAsync(int memberId, int loanId, CancellationToken ct);
    Task<ServiceResult<LoanResponse>> GetByIdAsync(int loanId, int memberId, bool isAdmin, CancellationToken ct);
    Task<ServiceResult<PagedResult<LoanResponse>>> GetMineAsync(int memberId, LoanQueryParameters q, CancellationToken ct);
    Task<ServiceResult<PagedResult<LoanResponse>>> GetAllAsync(LoanQueryParameters q, CancellationToken ct);
}
