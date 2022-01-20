// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up configuration for host.
    /// </summary>
    public static class ConfigurationHostBuilderExtensions
    {
        /// <summary>
        /// Gets the <see cref="IConfiguration"/> object configured using the "<see cref="UseDefaultConfiguration"/>.
        /// </summary>
        public static IConfiguration Configuration { get; }

        static ConfigurationHostBuilderExtensions()
        {
            Configuration = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                  .Build();
        }

        /// <summary>
        /// Use default configuration.
        /// </summary>
        /// <param name="builder"><see cref="IHostBuilder"/> to use with.</param>
        /// <returns><see cref="IHostBuilder"/> for continuation.</returns>
        public static IHostBuilder UseDefaultConfiguration(this IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(_ =>
            {
                _.Sources.Clear();
                _.AddConfiguration(Configuration);
            });

            return builder;
        }

        /// <summary>
        /// Use configuration objects through discovery based on objects adorned with <see cref="ConfigurationAttribute"/>.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to use with.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="baseRelativePath">Optional base relative path, relative to the current running directory.</param>
        /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
        public static IServiceCollection AddConfigurationObjects(this IServiceCollection services, ITypes types, string baseRelativePath = "")
        {
            foreach (var configurationObject in types.All.Where(_ => _.HasAttribute<ConfigurationAttribute>()))
            {
                var attribute = configurationObject.GetCustomAttribute<ConfigurationAttribute>()!;
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), baseRelativePath);

                var fileName = attribute.FileNameSet ? attribute.FileName : configurationObject.Name.ToLowerInvariant();
                fileName = Path.HasExtension(fileName) ? fileName : $"{fileName}.json";

                if (!attribute.Optional && !File.Exists(Path.Combine(basePath, fileName)))
                {
                    continue;
                }

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile(fileName, attribute.Optional)
                    .Build();

                var configurationInstance = configuration.Get(configurationObject);
                services.AddSingleton(configurationObject, configurationInstance);

                var optionsType = typeof(IOptions<>).MakeGenericType(configurationObject);
                var optionsWrapperType = typeof(OptionsWrapper<>).MakeGenericType(configurationObject);
                var optionsWrapperInstance = Activator.CreateInstance(optionsWrapperType, new[] { configurationInstance });

                services.AddSingleton(optionsType, optionsWrapperInstance!);
            }

            return services;
        }
    }
}
