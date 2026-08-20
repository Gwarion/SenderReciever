namespace Sender.Api;

static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers sender services, command handling, and HTTP upload repositories.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSenderApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<Program>());

        services.AddSingleton<UploadLineFormatter>();
        services.AddScoped<IDatabaseRepository, FakeDatabaseRepository>();

        services.AddHttpClient<IUploadRepository, HttpUploadRepository>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        return services;
    }
}
