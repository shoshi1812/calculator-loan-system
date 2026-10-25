using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Services;
using CalculatorLending.Core.Options;
using CalculatorLending.Data.Extensions;
using CalculatorLending.Data.Repositories;
using CalculatorLending.Service.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalculatorLending.Tests.Integration;

[Collection("Postgres")]
public class ConcurrencyTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    public ConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // One item with a single available copy, and the requested number of members
    private async Task<(int ItemId, int CopyId, List<int> MemberIds)> SeedAsync(int memberCount)
    {
        await using var context = _fixture.CreateContext();

        var item = new Item { Name = "Casio FX-991EX", ModelNumber = "FX-991EX" };
        var copy = new ItemCopy { SerialNumber = "SN-001", Status = CopyStatus.Available, Item = item };
        var members = Enumerable.Range(1, memberCount)
            .Select(i => new Member
            {
                FullName = $"Member {i}",
                Email = $"member{i}@example.com",
                PasswordHash = "not-a-real-hash",
                Role = MemberRole.Member
            })
            .ToList();

        context.Items.Add(item);
        context.ItemCopies.Add(copy);
        context.Members.AddRange(members);
        await context.SaveChangesAsync();

        return (item.Id, copy.Id, members.Select(m => m.Id).ToList());
    }

    private static Loan NewLoan(int memberId, int copyId) => new()
    {
        MemberId = memberId,
        ItemCopyId = copyId,
        LoanDate = DateTime.UtcNow,
        DueDate = DateTime.UtcNow.AddMonths(6),
        ReturnDate = null
    };

    [Fact]
    public async Task TwoContexts_SameCopy_SecondSaveThrowsConflict()
    {
        var (_, copyId, memberIds) = await SeedAsync(memberCount: 2);
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        var firstUnitOfWork = new UnitOfWork(first);
        var secondUnitOfWork = new UnitOfWork(second);
        var firstCopy = await first.ItemCopies.SingleAsync(c => c.Id == copyId);
        var secondCopy = await second.ItemCopies.SingleAsync(c => c.Id == copyId);

        firstCopy.Status = CopyStatus.Loaned;
        first.Loans.Add(NewLoan(memberIds[0], copyId));
        await firstUnitOfWork.SaveChangesAsync(CancellationToken.None);
        // Only the copy update, so the conflict can be caught only by xmin
        // (adding a loan here lets the unique index catch it first)
        secondCopy.Status = CopyStatus.Loaned;
        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => secondUnitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);
        await using var check = _fixture.CreateContext();
        Assert.Equal(1, await check.Loans.CountAsync());
    }

    [Fact]
    public async Task ParallelBorrow_LastCopy_ExactlyOneSucceeds()
    {
        var (itemId, copyId, memberIds) = await SeedAsync(memberCount: 8);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _fixture.ConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataLayer(configuration);
        services.AddServiceLayer();
        services.Configure<LoanOptions>(o =>
        {
            o.MaxActiveLoansPerMember = 2;
            o.LoanPeriodMonths = 6;
        });
        await using var provider = services.BuildServiceProvider();

        var results = await Task.WhenAll(memberIds.Select(async memberId =>
        {
            await using var scope = provider.CreateAsyncScope();
            var loanService = scope.ServiceProvider.GetRequiredService<ILoanService>();
            return await loanService.CreateAsync(memberId, new LoanCreateRequest { ItemId = itemId }, CancellationToken.None);
        }));

        Assert.Equal(1, results.Count(r => r.Status == ResultStatus.Success));
        Assert.Equal(7, results.Count(r => r.Status == ResultStatus.Conflict));
        await using var check = _fixture.CreateContext();
        Assert.Equal(1, await check.Loans.CountAsync(l => l.ReturnDate == null));
        var copy = await check.ItemCopies.SingleAsync(c => c.Id == copyId);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
    }

    [Fact]
    public async Task SecondActiveLoanOnSameCopy_ThrowsConflict()
    {
        var (_, copyId, memberIds) = await SeedAsync(memberCount: 2);
        await using var context = _fixture.CreateContext();
        var unitOfWork = new UnitOfWork(context);

        context.Loans.Add(NewLoan(memberIds[0], copyId));
        context.Loans.Add(NewLoan(memberIds[1], copyId));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));
    }
}