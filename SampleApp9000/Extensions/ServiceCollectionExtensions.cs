using Amazon.Extensions.NETCore.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp9000.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static IServiceCollection AddDefaultAWSOptions(this IServiceCollection collection)
        {
            collection
                .Add
                (
                    new ServiceDescriptor
                    (
                        typeof(AWSOptions),
                        provider => provider
                                            .GetService<IConfiguration>()
                                            .GetAWSOptions(),
                        ServiceLifetime.Singleton
                    )
                );

            return collection;
        }
    }
}
