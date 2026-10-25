using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Repositories;
using CalculatorLending.Core.Options;
using CalculatorLending.Service.Mapping;
using CalculatorLending.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace CalculatorLending.Tests.Unit;

public class LoanServiceTests
{
    private const int MemberId = 1;
    private const int OtherMemberId = 2;
    private const int ItemId = 10;
    private const int CopyId = 100;
    private const int LoanId = 1000;

    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<IItemRepository> _items = new();
    private readonly Mock<IItemCopyRepository> _copies = new();
    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IMapper _mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    private Loan? _addedLoan;

    private LoanService CreateService() => new(
        _loans.Object,
        _items.Object,
        _copies.Object,
        _members.Object,
        _unitOfWork.Object,
        _mapper,
        Microsoft.Extensions.Options.Options.Create(new LoanOptions { MaxActiveLoansPerMember = 2, LoanPeriodMonths = 6 }),
        _timeProvider,
        NullLogger<LoanService>.Instance);

    private static Item CreateItem() => new()
    {
        Id = ItemId,
        Name = "Casio FX-991EX",
        ModelNumber = "FX-991EX"
    };

    private static ItemCopy CreateCopy(CopyStatus status) => new()
    {
        Id = CopyId,
        ItemId = ItemId,
        SerialNumber = "SN-001",
        Status = status,
        Item = CreateItem()
    };

    private static Loan CreateActiveLoan(int memberId) => new()
    {
        Id = LoanId,
        MemberId = memberId,
        ItemCopyId = CopyId,
        LoanDate = Now.UtcDateTime,
        DueDate = Now.UtcDateTime.AddMonths(6),
        ReturnDate = null,
        ItemCopy = CreateCopy(CopyStatus.Loaned)
    };

    // Everything CreateAsync needs to succeed; each test overrides one setup
    private void SetupCreateHappyPath(ItemCopy copy)
    {
        _members.Setup(m => m.GetByIdAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = MemberId });
        _items.Setup(i => i.GetWithAvailabilityAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemWithAvailability(copy.Item, 1));
        _loans.Setup(l => l.CountActiveByMemberAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _copies.Setup(c => c.GetFirstAvailableAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(copy);
        _loans.Setup(l => l.Add(It.IsAny<Loan>()))
            .Callback<Loan>(l =>
            {
                l.ItemCopy = copy;
                _addedLoan = l;
            });
        _loans.Setup(l => l.GetDetailsByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _addedLoan);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private void SetupExistingLoan(Loan loan)
    {
        _loans.Setup(l => l.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);
        _loans.Setup(l => l.GetDetailsByIdAsync(LoanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);
    }

    private static LoanCreateRequest Request() => new() { ItemId = ItemId };

    [Fact]
    public async Task CreateAsync_CopyAvailable_Succeeds()
    {
        var copy = CreateCopy(CopyStatus.Available);
        SetupCreateHappyPath(copy);
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
        _loans.Verify(l => l.Add(It.IsAny<Loan>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsDueDateSixMonthsAhead()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        var service = CreateService();

        await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.NotNull(_addedLoan);
        Assert.Equal(Now.UtcDateTime, _addedLoan.LoanDate);
        Assert.Equal(new DateTime(2027, 4, 18, 12, 0, 0, DateTimeKind.Utc), _addedLoan.DueDate);
    }

    [Fact]
    public async Task CreateAsync_MemberAtLimit_ReturnsConflict()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        _loans.Setup(l => l.CountActiveByMemberAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NoCopyAvailable_ReturnsConflict()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        _copies.Setup(c => c.GetFirstAvailableAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemCopy?)null);
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ItemMissing_ReturnsNotFound()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        _items.Setup(i => i.GetWithAvailabilityAsync(ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemWithAvailability?)null);
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CreateAsync_MemberMissing_ReturnsUnauthorized()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        _members.Setup(m => m.GetByIdAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task CreateAsync_ConcurrentUpdate_ReturnsConflict()
    {
        SetupCreateHappyPath(CreateCopy(CopyStatus.Available));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("Concurrency conflict"));
        var service = CreateService();

        var result = await service.CreateAsync(MemberId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task ReturnAsync_OwnActiveLoan_Succeeds()
    {
        var loan = CreateActiveLoan(MemberId);
        SetupExistingLoan(loan);
        _timeProvider.Advance(TimeSpan.FromDays(30));
        var service = CreateService();

        var result = await service.ReturnAsync(MemberId, LoanId, CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
        Assert.Equal(CopyStatus.Available, loan.ItemCopy.Status);
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, loan.ReturnDate);
    }

    [Fact]
    public async Task ReturnAsync_OtherMembersLoan_ReturnsForbidden()
    {
        var loan = CreateActiveLoan(OtherMemberId);
        SetupExistingLoan(loan);
        var service = CreateService();

        var result = await service.ReturnAsync(MemberId, LoanId, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(CopyStatus.Loaned, loan.ItemCopy.Status);
    }

    [Fact]
    public async Task ReturnAsync_AlreadyReturned_ReturnsConflict()
    {
        var loan = CreateActiveLoan(MemberId);
        loan.ReturnDate = Now.UtcDateTime.AddDays(-1);
        SetupExistingLoan(loan);
        var service = CreateService();

        var result = await service.ReturnAsync(MemberId, LoanId, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task ReturnAsync_LoanMissing_ReturnsNotFound()
    {
        _loans.Setup(l => l.GetByIdAsync(LoanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);
        var service = CreateService();

        var result = await service.ReturnAsync(MemberId, LoanId, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonAdminOtherMember_ReturnsForbidden()
    {
        SetupExistingLoan(CreateActiveLoan(OtherMemberId));
        var service = CreateService();

        var result = await service.GetByIdAsync(LoanId, MemberId, isAdmin: false, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_AdminOtherMember_ReturnsSuccess()
    {
        SetupExistingLoan(CreateActiveLoan(OtherMemberId));
        var service = CreateService();

        var result = await service.GetByIdAsync(LoanId, MemberId, isAdmin: true, CancellationToken.None);

        Assert.Equal(ResultStatus.Success, result.Status);
    }
}