using AutoMapper;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Service.Mapping;

namespace CalculatorLending.Tests.Unit;

public class MappingTests
{
    private readonly MapperConfiguration _configuration =
        new(cfg => cfg.AddProfile<MappingProfile>());

    private LoanResponse MapLoan(DateTime dueDate, DateTime? returnDate)
    {
        var loan = new Loan
        {
            Id = 1,
            MemberId = 1,
            ItemCopyId = 100,
            LoanDate = dueDate.AddMonths(-6),
            DueDate = dueDate,
            ReturnDate = returnDate,
            ItemCopy = new ItemCopy
            {
                Id = 100,
                ItemId = 10,
                SerialNumber = "SN-001",
                Status = returnDate is null ? CopyStatus.Loaned : CopyStatus.Available,
                Item = new Item { Id = 10, Name = "Casio FX-991EX", ModelNumber = "FX-991EX" }
            }
        };

        return _configuration.CreateMapper().Map<LoanResponse>(loan);
    }

    [Fact]
    public void Configuration_IsValid()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_ActiveLoanDueYesterday_IsOverdue()
    {
        var response = MapLoan(DateTime.UtcNow.AddDays(-1), returnDate: null);

        Assert.True(response.IsOverdue);
    }

    [Fact]
    public void Map_ActiveLoanDueInFuture_IsNotOverdue()
    {
        var response = MapLoan(DateTime.UtcNow.AddMonths(1), returnDate: null);

        Assert.False(response.IsOverdue);
    }

    [Fact]
    public void Map_ReturnedLoanDueYesterday_IsNotOverdue()
    {
        var response = MapLoan(DateTime.UtcNow.AddDays(-1), returnDate: DateTime.UtcNow.AddDays(-2));

        Assert.False(response.IsOverdue);
    }
}