using FlowR.Mapper.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowR.Mapper.Extensions;

/// <summary>
/// Extension methods to register FlowR.Mapper with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class FlowRMapperServiceCollectionExtensions
{
    /// <summary>
    /// Registers FlowR.Mapper, scans the given assemblies for <see cref="MapperProfile"/> implementations,
    /// and registers IMapper as a singleton.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for profiles.</param>
    public static IServiceCollection AddFlowRMapper(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        return services.AddFlowRMapper(_ => { }, assemblies);
    }

    /// <summary>
    /// Registers FlowR.Mapper with configuration options.
    /// </summary>
    public static IServiceCollection AddFlowRMapper(
        this IServiceCollection services,
        Action<FlowRMapperOptions> configure,
        params Assembly[] assemblies)
    {
        var options = new FlowRMapperOptions();
        configure(options);

        var registry = new MapperRegistry();
        var configurator = new ProfileConfigurator(registry);

        if (assemblies.Length == 0)
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry != null) assemblies = [entry];
        }

        // Discover and apply all profiles from assemblies
        foreach (var assembly in assemblies.Distinct())
        {
            var profileTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IMapperProfile).IsAssignableFrom(t));

            foreach (var profileType in profileTypes)
            {
                var profile = (IMapperProfile)Activator.CreateInstance(profileType)!;
                profile.Configure(configurator);
            }
        }

        // Apply profiles provided via options
        foreach (var profile in options.Profiles)
        {
            profile.Configure(configurator);
        }

        var mapper = new FlowRMapper(registry);

        // Validate at startup if requested
        if (options.ValidateOnStartup)
            mapper.AssertConfigurationIsValid();

        services.AddSingleton<IMapper>(mapper);
        services.AddSingleton(mapper);

        return services;
    }

    /// <summary>
    /// Manually add a profile without assembly scanning.
    /// </summary>
    public static IServiceCollection AddFlowRMapper(
        this IServiceCollection services,
        Action<IProfileConfigurator> configure)
    {
        var registry = new MapperRegistry();
        var configurator = new ProfileConfigurator(registry);
        configure(configurator);

        services.AddSingleton<IMapper>(new FlowRMapper(registry));
        return services;
    }
}

/// <summary>
/// Configuration options for FlowR.Mapper.
/// </summary>
public sealed class FlowRMapperOptions
{
    /// <summary>
    /// If true, calls AssertConfigurationIsValid() on startup.
    /// Recommended for production apps to catch config errors early.
    /// Default: false.
    /// </summary>
    public bool ValidateOnStartup { get; set; }

    /// <summary>
    /// Profiles to register manually (in addition to assembly scanning).
    /// </summary>
    public List<IMapperProfile> Profiles { get; } = [];

    public FlowRMapperOptions AddProfile<TProfile>() where TProfile : IMapperProfile, new()
    {
        Profiles.Add(new TProfile());
        return this;
    }

    public FlowRMapperOptions AddProfile(IMapperProfile profile)
    {
        Profiles.Add(profile);
        return this;
    }
}
