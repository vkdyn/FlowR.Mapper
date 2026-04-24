namespace FlowR.Mapper.Tests;

using FlowR.Mapper;
using FlowR.Mapper.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests AutoMapper-style compatibility extension methods used during migration.
/// </summary>
public sealed class AutoMapperCompatibilityExtensionsTests
{
    [Fact]
    public void ForAllMembers_Condition_DoesNotOverwriteExistingDestinationValueWithNullSourceMember()
    {
        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForAllMembers(options =>
                {
                    options.AllowNull();
                    options.Condition((source, destination, sourceMember, destinationMember) => sourceMember != null);
                });
        });

        SourceModel source = new()
        {
            Name = null,
            Description = "Updated description"
        };

        DestinationModel destination = new()
        {
            Name = "Existing name",
            Description = "Existing description"
        };

        DestinationModel result = mapper.Map(source, destination);

        Assert.Same(destination, result);
        Assert.Equal("Existing name", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public void ForMember_Condition_DoesNotOverwriteExistingDestinationValueWithNullSourceMember()
    {
        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForMember(
                    destination => destination.Name,
                    options => options.Condition((source, destination, sourceMember, destinationMember) => sourceMember != null));
        });

        SourceModel source = new()
        {
            Name = null,
            Description = "Updated description"
        };

        DestinationModel destination = new()
        {
            Name = "Existing name",
            Description = "Existing description"
        };

        DestinationModel result = mapper.Map(source, destination);

        Assert.Same(destination, result);
        Assert.Equal("Existing name", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public void ForMember_Condition_ReceivesCurrentDestinationMemberValue()
    {
        string? capturedDestinationMember = null;

        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForMember(
                    destination => destination.Name,
                    options => options.Condition((source, destination, sourceMember, destinationMember) =>
                    {
                        capturedDestinationMember = destinationMember;
                        return sourceMember != null;
                    }));
        });

        SourceModel source = new()
        {
            Name = "Updated name"
        };

        DestinationModel destination = new()
        {
            Name = "Existing name"
        };

        DestinationModel result = mapper.Map(source, destination);

        Assert.Same(destination, result);
        Assert.Equal("Existing name", capturedDestinationMember);
        Assert.Equal("Updated name", result.Name);
    }

    [Fact]
    public void PreCondition_WithSourceDestinationContextSignature_SkipsMemberWhenFalse()
    {
        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForMember(
                    destination => destination.Name,
                    options => options.PreCondition((source, destination, context) => source.Name != null));
        });

        SourceModel source = new()
        {
            Name = null,
            Description = "Updated description"
        };

        DestinationModel destination = new()
        {
            Name = "Existing name",
            Description = "Existing description"
        };

        DestinationModel result = mapper.Map(source, destination);

        Assert.Same(destination, result);
        Assert.Equal("Existing name", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public void PreCondition_WithSourceDestinationSignature_SkipsMemberWhenFalse()
    {
        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForMember(
                    destination => destination.Name,
                    options => options.PreCondition((source, destination) => source.Name != null));
        });

        SourceModel source = new()
        {
            Name = null,
            Description = "Updated description"
        };

        DestinationModel destination = new()
        {
            Name = "Existing name",
            Description = "Existing description"
        };

        DestinationModel result = mapper.Map(source, destination);

        Assert.Same(destination, result);
        Assert.Equal("Existing name", result.Name);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public void MapFrom_WithResolverType_MapsUsingResolver()
    {
        IMapper mapper = BuildMapper(config =>
        {
            config.CreateMap<SourceModel, DestinationModel>()
                .ForMember(
                    destination => destination.Name,
                    options => options.MapFrom<SourceNameResolver>());
        });

        SourceModel source = new()
        {
            Name = "qa"
        };

        DestinationModel result = mapper.Map<SourceModel, DestinationModel>(source);

        Assert.Equal("Resolved-qa", result.Name);
    }

    private static IMapper BuildMapper(Action<IProfileConfigurator> configure)
    {
        ServiceCollection services = new();

        services.AddFlowRMapper(configure);

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IMapper>();
    }

    private sealed class SourceModel
    {
        public string? Name { get; set; }

        public string? Description { get; set; }
    }

    private sealed class DestinationModel
    {
        public string? Name { get; set; }

        public string? Description { get; set; }
    }

    private sealed class SourceNameResolver : IValueResolver<SourceModel, DestinationModel, string?>
    {
        public string? Resolve(
            SourceModel source,
            DestinationModel destination,
            string? destinationMember,
            ResolutionContext context)
        {
            return $"Resolved-{source.Name}";
        }
    }
}
