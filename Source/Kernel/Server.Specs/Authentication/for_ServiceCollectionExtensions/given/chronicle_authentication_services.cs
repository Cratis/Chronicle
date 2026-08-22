// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Server.Authentication.OpenIddict;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_ServiceCollectionExtensions.given;

public class chronicle_authentication_services : Specification
{
    protected ChronicleAuthenticationServices BuildServices(SharedDataProtectionKeys? sharedKeys = null)
    {
        sharedKeys ??= new();
        var dataProtectionKeys = sharedKeys.CreateGrain();
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IDataProtectionKeys>(Arg.Any<string>()).Returns(dataProtectionKeys);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(grainFactory);
        services.AddChronicleAuthentication(new Cratis.Chronicle.Configuration.ChronicleOptions
        {
            Authentication = new Cratis.Chronicle.Configuration.Authentication
            {
                Enabled = false
            }
        });

        var serviceProvider = services.BuildServiceProvider();

        return new(
            serviceProvider,
            grainFactory,
            dataProtectionKeys,
            sharedKeys,
            serviceProvider.GetRequiredService<GrainBasedXmlRepository>(),
            serviceProvider.GetRequiredService<IXmlRepository>(),
            serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>(),
            serviceProvider.GetRequiredService<IDataProtectionProvider>());
    }

    protected sealed record ChronicleAuthenticationServices(
        ServiceProvider ServiceProvider,
        IGrainFactory GrainFactory,
        IDataProtectionKeys DataProtectionKeys,
        SharedDataProtectionKeys SharedKeys,
        GrainBasedXmlRepository ConcreteRepository,
        IXmlRepository XmlRepository,
        IOptions<KeyManagementOptions> KeyManagementOptions,
        IDataProtectionProvider DataProtectionProvider);

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
