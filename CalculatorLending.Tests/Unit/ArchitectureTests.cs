using System.Reflection;
using CalculatorLending.Core.Entities;
using CalculatorLending.Data.Context;
using CalculatorLending.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace CalculatorLending.Tests.Unit;

public class ArchitectureTests
{
    private const string Core = "CalculatorLending.Core";
    private const string Data = "CalculatorLending.Data";
    private const string Service = "CalculatorLending.Service";
    private const string Api = "CalculatorLending.API";
    private const string EfCorePrefix = "Microsoft.EntityFrameworkCore";
    private const string RepositoriesNamespace = "CalculatorLending.Core.Interfaces.Repositories";

    private static List<string> ReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    [Fact]
    public void Core_DoesNotDependOnOtherProjectsOrEfCore()
    {
        var references = ReferencesOf(typeof(Item).Assembly);

        Assert.DoesNotContain(references, r => r.StartsWith(EfCorePrefix));
        Assert.DoesNotContain(Data, references);
        Assert.DoesNotContain(Service, references);
        Assert.DoesNotContain(Api, references);
    }

    [Fact]
    public void Service_DependsOnCoreOnly()
    {
        var references = ReferencesOf(typeof(LoanService).Assembly);

        Assert.Contains(Core, references);
        Assert.DoesNotContain(Data, references);
        Assert.DoesNotContain(Api, references);
        Assert.DoesNotContain(references, r => r.StartsWith(EfCorePrefix));
    }

    [Fact]
    public void Data_DependsOnCoreAndNotOnServiceOrApi()
    {
        var references = ReferencesOf(typeof(AppDbContext).Assembly);

        Assert.Contains(Core, references);
        Assert.DoesNotContain(Service, references);
        Assert.DoesNotContain(Api, references);
    }

    [Fact]
    public void Api_DependsOnCoreAndService()
    {
        // A reference to Data is allowed, only for the registration in Program.cs
        var references = ReferencesOf(typeof(Program).Assembly);

        Assert.Contains(Core, references);
        Assert.Contains(Service, references);
    }

    [Fact]
    public void Controllers_DoNotInjectDbContextOrRepositories()
    {
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ControllerBase)))
            .ToList();

        Assert.NotEmpty(controllers);

        var violations = controllers
            .SelectMany(c => c.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters())
                .Where(p => p.ParameterType == typeof(AppDbContext)
                         || p.ParameterType.Namespace == RepositoriesNamespace)
                .Select(p => $"{c.Name}({p.ParameterType.Name})"))
            .ToList();

        Assert.Empty(violations);
    }
}