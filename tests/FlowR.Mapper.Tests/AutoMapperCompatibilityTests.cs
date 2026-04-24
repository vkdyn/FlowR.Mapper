namespace FlowR.Mapper.Tests;

using FlowR.Mapper;
using FlowR.Mapper.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Covers the AutoMapper-like compatibility APIs used by large migrated profiles.
/// </summary>
public sealed class AutoMapperCompatibilityTests
{
    [Fact]
    public void ConvertUsing_WithSourceAndDestinationLambda_UsesExistingDestinationValue()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<int?, int>()
                .ConvertUsing((src, dest) => src ?? dest);
        });

        int result = mapper.Map<int?, int>(null, 7);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ForAllMembers_WithNullCondition_DoesNotOverwriteExistingDestinationValues()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PatchSource, PatchDestination>()
                .ForAllMembers(opt =>
                {
                    opt.AllowNull();
                    opt.Condition((src, dest, srcMember, destMember) => srcMember != null);
                });
        });

        PatchSource source = new()
        {
            Name = null,
            Description = "Updated"
        };

        PatchDestination destination = new()
        {
            Name = "Keep me",
            Description = "Old"
        };

        PatchDestination result = mapper.Map(source, destination);

        Assert.Equal("Keep me", result.Name);
        Assert.Equal("Updated", result.Description);
    }

    [Fact]
    public void IncludeBase_MapsBaseConfiguration()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<BaseEntity, BaseDto>()
                .ForMember(dest => dest.Identifier, opt => opt.MapFrom(src => src.Id));

            cfg.CreateMap<ChildEntity, ChildDto>()
                .IncludeBase<BaseEntity, BaseDto>();
        });

        ChildDto result = mapper.Map<ChildEntity, ChildDto>(new ChildEntity
        {
            Id = 22,
            Name = "Child"
        });

        Assert.Equal(22, result.Identifier);
        Assert.Equal("Child", result.Name);
    }

    [Fact]
    public void MemberPreCondition_SkipsMemberWhenFalse()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PatchSource, PatchDestination>()
                .ForMember(dest => dest.Name, opt =>
                {
                    opt.PreCondition((src, dest, context) => src.Name != null);
                    opt.MapFrom(src => src.Name!);
                });
        });

        PatchDestination destination = new()
        {
            Name = "Existing"
        };

        PatchDestination result = mapper.Map(new PatchSource { Name = null }, destination);

        Assert.Equal("Existing", result.Name);
    }

    [Fact]
    public void AfterMap_WithResolutionContext_RunsAfterMapping()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PatchSource, PatchDestination>()
                .AfterMap((src, dest, context) =>
                {
                    dest.ContextSourceType = context.SourceType.Name;
                    dest.ContextDestinationType = context.DestinationType.Name;
                });
        });

        PatchDestination result = mapper.Map<PatchSource, PatchDestination>(new PatchSource
        {
            Name = "A"
        });

        Assert.Equal(nameof(PatchSource), result.ContextSourceType);
        Assert.Equal(nameof(PatchDestination), result.ContextDestinationType);
    }

    [Fact]
    public void ProjectTo_Extension_IsAvailableFromRootNamespace()
    {
        IMapper mapper = BuildMapper(cfg =>
        {
            cfg.CreateMap<PatchSource, PatchDestination>();
        });

        List<PatchDestination> result = new List<PatchSource>
        {
            new PatchSource { Name = "One", Description = "First" }
        }
        .AsQueryable()
        .ProjectTo<PatchSource, PatchDestination>(mapper)
        .ToList();

        Assert.Single(result);
        Assert.Equal("One", result[0].Name);
    }

    private static IMapper BuildMapper(Action<IProfileConfigurator> configure)
    {
        ServiceCollection services = new();
        services.AddFlowRMapper(configure);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private sealed class PatchSource
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private sealed class PatchDestination
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ContextSourceType { get; set; }
        public string? ContextDestinationType { get; set; }
    }

    private class BaseEntity
    {
        public int Id { get; set; }
    }

    private class BaseDto
    {
        public int Identifier { get; set; }
    }

    private sealed class ChildEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ChildDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
