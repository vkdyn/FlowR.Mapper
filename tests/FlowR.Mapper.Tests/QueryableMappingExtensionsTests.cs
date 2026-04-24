namespace FlowR.Mapper.Tests;

using FlowR.Mapper.Extensions;
using FlowR.Mapper.Interfaces;
using FlowR.Mapper.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for IQueryable extension methods.
/// </summary>
public sealed class QueryableMappingExtensionsTests
{
    [Fact]
    public void ProjectTo_WithValidQueryable_ProjectsSourceToDestination()
    {
        IMapper mapper = BuildMapper();

        IQueryable<OrderEntity> query = CreateOrders().AsQueryable();

        List<OrderDto> result = query
            .ProjectTo<OrderEntity, OrderDto>(mapper)
            .ToList();

        Assert.Equal(2, result.Count);

        Assert.Equal(1, result[0].OrderId);
        Assert.Equal(25.50m, result[0].Total);
        Assert.Equal("Pending", result[0].Status);

        Assert.Equal(2, result[1].OrderId);
        Assert.Equal(99.99m, result[1].Total);
        Assert.Equal("Complete", result[1].Status);
    }

    [Fact]
    public void ProjectTo_WithWhereBeforeProjection_FiltersThenProjects()
    {
        IMapper mapper = BuildMapper();

        IQueryable<OrderEntity> query = CreateOrders().AsQueryable();

        OrderDto? result = query
            .Where(x => x.OrderId == 2)
            .ProjectTo<OrderEntity, OrderDto>(mapper)
            .FirstOrDefault();

        Assert.NotNull(result);
        Assert.Equal(2, result.OrderId);
        Assert.Equal(99.99m, result.Total);
        Assert.Equal("Complete", result.Status);
    }

    [Fact]
    public void ProjectTo_WithNullSource_ThrowsArgumentNullException()
    {
        IMapper mapper = BuildMapper();

        IQueryable<OrderEntity>? query = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            query!.ProjectTo<OrderEntity, OrderDto>(mapper));

        Assert.Equal("source", exception.ParamName);
    }

    [Fact]
    public void ProjectTo_WithNullMapper_ThrowsArgumentNullException()
    {
        IQueryable<OrderEntity> query = CreateOrders().AsQueryable();

        IMapper? mapper = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            query.ProjectTo<OrderEntity, OrderDto>(mapper!));

        Assert.Equal("mapper", exception.ParamName);
    }

    private static IMapper BuildMapper()
    {
        ServiceCollection services = new();

        services.AddFlowRMapper(config =>
        {
            config.CreateMap<OrderEntity, OrderDto>();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<IMapper>();
    }

    private static List<OrderEntity> CreateOrders()
    {
        return
        [
            new OrderEntity
            {
                OrderId = 1,
                Total = 25.50m,
                Status = "Pending"
            },
            new OrderEntity
            {
                OrderId = 2,
                Total = 99.99m,
                Status = "Complete"
            }
        ];
    }
}