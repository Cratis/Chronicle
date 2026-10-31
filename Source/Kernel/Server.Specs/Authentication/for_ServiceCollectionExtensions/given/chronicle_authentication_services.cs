// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Chronicle.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

public class chronicle_authentication_services : for_CertificateLoader.given.a_certificate_file
{
    protected ChronicleAuthenticationServices BuildServices(
        SharedDataProtectionKeys? sharedKeys = null,
        bool authenticationEnabled = false,
        bool useInternalAuthority = false,
        Action<IServiceCollection>? configureServices = null)
    {
        sharedKeys ??= new();
        var dataProtectionKeys = sharedKeys.CreateGrain();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IDataProtectionKeys>(Arg.Any<string>()).Returns(dataProtectionKeys);

        if (authenticationEnabled && useInternalAuthority)
        {
            // Exercise the production certificate requirement in both build configurations.
            WritePkcs12(null);
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(grainFactory);
        services.AddSingleton(Substitute.For<Storage.ISystemStorage>());
        services.AddChronicleAuthentication(new Configuration.ChronicleOptions
        {
            Storage = new() { Type = Storage.StorageType.InMemory },
            EncryptionCertificate = new()
            {
                CertificatePath = authenticationEnabled && useInternalAuthority ? _certificatePath : null
            },
            Authentication = new Configuration.Authentication
            {
                Enabled = authenticationEnabled,
                Authority = authenticationEnabled && !useInternalAuthority ? "https://identity.example" : null
            }
        });

        configureServices?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();

        return new(
            serviceProvider,
            grainFactory,
            dataProtectionKeys,
            sharedKeys,
            serviceProvider.GetRequiredService<GrainBasedXmlRepository>(),
            serviceProvider.GetRequiredService<IXmlRepository>(),
            serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>(),
            serviceProvider.GetRequiredService<IDataProtectionProvider>(),
            services);
    }

    protected sealed record ChronicleAuthenticationServices(
        ServiceProvider ServiceProvider,
        IGrainFactory GrainFactory,
        IDataProtectionKeys DataProtectionKeys,
        SharedDataProtectionKeys SharedKeys,
        GrainBasedXmlRepository ConcreteRepository,
        IXmlRepository XmlRepository,
        IOptions<KeyManagementOptions> KeyManagementOptions,
        IDataProtectionProvider DataProtectionProvider,
        IServiceCollection Services);

    protected sealed class SharedDataProtectionKeys
    {
        readonly object _gate = new();
        readonly Dictionary<string, string> _keys = [];

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _keys.Count;
                }
            }
        }

        public string[] Snapshot()
        {
            lock (_gate)
            {
                return [.. _keys.Values];
            }
        }

        public IDataProtectionKeys CreateGrain()
        {
            var grain = Substitute.For<IDataProtectionKeys>();
            grain.GetAllKeys().Returns(_ =>
            {
                lock (_gate)
                {
                    return Task.FromResult<IEnumerable<string>>([.. _keys.Values]);
                }
            });
            grain.StoreKey(Arg.Any<string>(), Arg.Any<string>()).Returns(callInfo =>
            {
                lock (_gate)
                {
                    _keys[(string)callInfo[0]] = (string)callInfo[1];
                }

                return Task.CompletedTask;
            });

            return grain;
        }
    }

    protected static string Normalize(XElement element) => element.ToString(SaveOptions.DisableFormatting);
}
