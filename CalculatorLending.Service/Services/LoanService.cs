using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Interfaces.Services;
using CalculatorLending.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalculatorLending.Service.Services;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loans;
    private readonly IItemRepository _items;
    private readonly IItemCopyRepository _copies;
    private readonly IMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly LoanOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LoanService> _logger;

    public LoanService(
        ILoanRepository loans,
        IItemRepository items,
        IItemCopyRepository copies,
        IMemberRepository members,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<LoanOptions> options,
        TimeProvider timeProvider,
        ILogger<LoanService> logger)
    {
        _loans = loans;
        _items = items;
        _copies = copies;
        _members = members;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult<LoanResponse>> CreateAsync(int memberId, LoanCreateRequest r, CancellationToken ct)
    {
        // 1. The member must exist
        var member = await _members.GetByIdAsync(memberId, ct);
        if (member is null)
        {
            _logger.LogWarning("Loan rejected: member {MemberId} does not exist", memberId);
            return ServiceResult<LoanResponse>.Unauthorized("Member does not exist");
        }

        // 2. The item must exist
        if (await _items.GetWithAvailabilityAsync(r.ItemId, ct) is null)
        {
            return ServiceResult<LoanResponse>.NotFound("Item not found");
        }

        // 3. The member must be under the active loans limit
        var activeLoans = await _loans.CountActiveByMemberAsync(memberId, ct);
        if (activeLoans >= _options.MaxActiveLoansPerMember)
        {
            _logger.LogWarning("Loan rejected: member {MemberId} reached the loan limit", memberId);
            return ServiceResult<LoanResponse>.Conflict("Loan limit reached");
        }

        // 4. A copy must be available
        var copy = await _copies.GetFirstAvailableAsync(r.ItemId, ct);
        if (copy is null)
        {
            _logger.LogWarning("Loan rejected: no copy of item {ItemId} is available", r.ItemId);
            return ServiceResult<LoanResponse>.Conflict("No copy is currently available");
        }

        // 5. The status change and the new loan are saved together in one save
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        copy.Status = CopyStatus.Loaned;

        var loan = new Loan
        {
            MemberId = memberId,
            ItemCopyId = copy.Id,
            LoanDate = now,
            DueDate = now.AddMonths(_options.LoanPeriodMonths),
            ReturnDate = null
        };
        _loans.Add(loan);

        // 6. Another request may have taken the same copy in the meantime
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Loan rejected: copy {CopyId} was taken by another request", copy.Id);
            return ServiceResult<LoanResponse>.Conflict(
                "The calculator was just taken by someone else. Please try again");
        }

        _logger.LogInformation("Loan {LoanId} created for member {MemberId}", loan.Id, memberId);

        // 7. Reload with the item details and map
        return await LoadResponseAsync(loan.Id, ct);
    }

    public async Task<ServiceResult<LoanResponse>> ReturnAsync(int memberId, int loanId, CancellationToken ct)
    {
        var loan = await _loans.GetByIdAsync(loanId, ct);
        if (loan is null)
        {
            return ServiceResult<LoanResponse>.NotFound("Loan not found");
        }

        if (loan.MemberId != memberId)
        {
            _logger.LogWarning("Return rejected: loan {LoanId} does not belong to member {MemberId}", loanId, memberId);
            return ServiceResult<LoanResponse>.Forbidden("This loan belongs to another member");
        }

        if (loan.ReturnDate is not null)
        {
            _logger.LogWarning("Return rejected: loan {LoanId} was already returned", loanId);
            return ServiceResult<LoanResponse>.Conflict("This loan was already returned");
        }

        loan.ReturnDate = _timeProvider.GetUtcNow().UtcDateTime;
        loan.ItemCopy.Status = CopyStatus.Available;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Return rejected: loan {LoanId} was changed by another request", loanId);
            return ServiceResult<LoanResponse>.Conflict("The loan was just changed. Please try again");
        }

        _logger.LogInformation("Loan {LoanId} returned by member {MemberId}", loanId, memberId);

        return await LoadResponseAsync(loanId, ct);
    }

    public async Task<ServiceResult<LoanResponse>> GetByIdAsync(int loanId, int memberId, bool isAdmin, CancellationToken ct)
    {
        var loan = await _loans.GetDetailsByIdAsync(loanId, ct);
        if (loan is null)
        {
            return ServiceResult<LoanResponse>.NotFound("Loan not found");
        }

        if (!isAdmin && loan.MemberId != memberId)
        {
            return ServiceResult<LoanResponse>.Forbidden("This loan belongs to another member");
        }

        return ServiceResult<LoanResponse>.Success(_mapper.Map<LoanResponse>(loan));
    }

    public async Task<ServiceResult<PagedResult<LoanResponse>>> GetMineAsync(int memberId, LoanQueryParameters q, CancellationToken ct)
    {
        var paged = await _loans.GetPagedByMemberAsync(memberId, q, ct);
        return ServiceResult<PagedResult<LoanResponse>>.Success(MapPaged(paged));
    }

    public async Task<ServiceResult<PagedResult<LoanResponse>>> GetAllAsync(LoanQueryParameters q, CancellationToken ct)
    {
        var paged = await _loans.GetPagedAsync(q, ct);
        return ServiceResult<PagedResult<LoanResponse>>.Success(MapPaged(paged));
    }

    private async Task<ServiceResult<LoanResponse>> LoadResponseAsync(int loanId, CancellationToken ct)
    {
        var loan = await _loans.GetDetailsByIdAsync(loanId, ct);
        if (loan is null)
        {
            return ServiceResult<LoanResponse>.NotFound("Loan not found");
        }

        return ServiceResult<LoanResponse>.Success(_mapper.Map<LoanResponse>(loan));
    }

    private PagedResult<LoanResponse> MapPaged(PagedResult<Loan> paged) => new()
    {
        Items = _mapper.Map<List<LoanResponse>>(paged.Items),
        Page = paged.Page,
        PageSize = paged.PageSize,
        TotalCount = paged.TotalCount
    };
}
