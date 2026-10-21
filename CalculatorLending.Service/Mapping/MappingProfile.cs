using AutoMapper;
using CalculatorLending.Core.Common;
using CalculatorLending.Core.DTOs.Categories;
using CalculatorLending.Core.DTOs.Copies;
using CalculatorLending.Core.DTOs.Items;
using CalculatorLending.Core.DTOs.Loans;
using CalculatorLending.Core.Entities;

namespace CalculatorLending.Service.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity -> response
        CreateMap<Category, CategoryResponse>();

        CreateMap<ItemCopy, CopyResponse>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<ItemWithAvailability, ItemResponse>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Item.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Item.Name))
            .ForMember(d => d.ModelNumber, o => o.MapFrom(s => s.Item.ModelNumber))
            .ForMember(d => d.Description, o => o.MapFrom(s => s.Item.Description))
            .ForMember(d => d.Categories, o => o.MapFrom(s => s.Item.Categories));
        // AvailableCopies is mapped by name from ItemWithAvailability.AvailableCopies

        CreateMap<Loan, LoanResponse>()
            .ForMember(d => d.ItemId, o => o.MapFrom(s => s.ItemCopy.ItemId))
            .ForMember(d => d.ItemName, o => o.MapFrom(s => s.ItemCopy.Item.Name))
            .ForMember(d => d.SerialNumber, o => o.MapFrom(s => s.ItemCopy.SerialNumber))
            .ForMember(d => d.IsOverdue, o => o.MapFrom(s => s.ReturnDate == null && s.DueDate < DateTime.UtcNow));

        // Request -> entity (relations are attached manually in the services)
        CreateMap<ItemCreateRequest, Item>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Categories, o => o.Ignore())
            .ForMember(d => d.Copies, o => o.Ignore());

        CreateMap<CopyCreateRequest, ItemCopy>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.ItemId, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Version, o => o.Ignore())
            .ForMember(d => d.Item, o => o.Ignore())
            .ForMember(d => d.Loans, o => o.Ignore());

        CreateMap<CategoryCreateRequest, Category>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Items, o => o.Ignore());
    }
}