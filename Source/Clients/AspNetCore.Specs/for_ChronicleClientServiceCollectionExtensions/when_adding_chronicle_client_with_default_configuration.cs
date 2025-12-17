// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore.Namespaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_adding_chronicle_client_with_default_configuration : Specification
{
    ServiceCollection _services;
    ServiceProvider _serviceProvider;
    IEventStoreNamespaceResolver _resolver;

    void Establish()
    {
        _services = new ServiceCollection();
        _services.Configure<ChronicleAspNetCoreOptions>(options => options.EventStore = "test-store");
    }

    void Because()
    {
        _services.AddCratisChronicleClient();
        _serviceProvider = _services.BuildServiceProvider();
        _resolver = _serviceProvider.GetRequiredService<IEventStoreNamespaceResolver>();
    }

    [Fact] void should_use_http_header_namespace_resolver() => _resolver.ShouldBeOfExactType<HttpHeaderEventStoreNamespaceResolver>();
}
