using CalculatorLending.Core.Entities;
using CalculatorLending.Core.Enums;
using CalculatorLending.Core.Interfaces.Data;
using CalculatorLending.Core.Interfaces.Security;
using CalculatorLending.Core.Options;
using CalculatorLending.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CalculatorLending.Data.Seed;

public class DatabaseInitializer : IDatabaseInitializer
{
    private static readonly string[] CategoryNames =
        { "Scientific", "Graphing", "Financial", "Programmable", "Basic" };

    private static readonly (string Name, string ModelNumber, string[] Categories, string[] Serials)[] DemoItems =
    {
        ("Casio fx-991EX", "FX-991EX", new[] { "Scientific" }, new[] { "FX991-001", "FX991-002", "FX991-003" }),
        ("TI-84 Plus CE", "TI-84PCE", new[] { "Graphing", "Programmable" }, new[] { "TI84-001", "TI84-002" }),
        ("HP 12C", "HP-12C", new[] { "Financial" }, new[] { "HP12C-001", "HP12C-002" })
    };

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedOptions _seedOptions;

    public DatabaseInitializer(AppDbContext context, IPasswordHasher passwordHasher, IOptions<SeedOptions> seedOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _seedOptions = seedOptions.Value;
    }

    public Task MigrateAsync(CancellationToken ct = default) =>
        _context.Database.MigrateAsync(ct);

    public async Task SeedReferenceDataAsync(CancellationToken ct = default)
    {
        foreach (var name in CategoryNames)
        {
            if (!await _context.Categories.AnyAsync(c => c.Name == name, ct))
            {
                _context.Categories.Add(new Category { Name = name });
            }
        }

        await AddMemberIfMissingAsync(_seedOptions.AdminFullName, _seedOptions.AdminEmail,
            _seedOptions.AdminPassword, MemberRole.Admin, ct);

        await _context.SaveChangesAsync(ct);
    }

    public async Task SeedDemoDataAsync(CancellationToken ct = default)
    {
        await AddMemberIfMissingAsync("Demo Member", _seedOptions.DemoMemberEmail,
            _seedOptions.DemoMemberPassword, MemberRole.Member, ct);

        // Relies on SeedReferenceDataAsync having created the categories
        var categories = await _context.Categories.ToDictionaryAsync(c => c.Name, ct);

        foreach (var demo in DemoItems)
        {
            var item = await _context.Items
                .FirstOrDefaultAsync(i => i.ModelNumber == demo.ModelNumber, ct);

            if (item is null)
            {
                item = new Item { Name = demo.Name, ModelNumber = demo.ModelNumber };
                foreach (var categoryName in demo.Categories)
                {
                    if (categories.TryGetValue(categoryName, out var category))
                    {
                        item.Categories.Add(category);
                    }
                }
                _context.Items.Add(item);
            }

            foreach (var serial in demo.Serials)
            {
                if (!await _context.ItemCopies.AnyAsync(c => c.SerialNumber == serial, ct))
                {
                    item.Copies.Add(new ItemCopy { SerialNumber = serial, Status = CopyStatus.Available });
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task AddMemberIfMissingAsync(string fullName, string email, string password,
        MemberRole role, CancellationToken ct)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _context.Members.AnyAsync(m => m.Email == normalizedEmail, ct))
        {
            return;
        }

        _context.Members.Add(new Member
        {
            FullName = fullName,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(password),
            Role = role
        });
    }
}