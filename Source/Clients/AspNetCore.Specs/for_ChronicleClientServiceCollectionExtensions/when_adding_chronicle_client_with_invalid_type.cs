// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_adding_chronicle_client_with_invalid_type : Specification
{
    ServiceCollection _services;
    ServiceProvider _serviceProvider;
    Exception _exception;

    void Establish()
    {
        _services = new ServiceCollection();
        _services.Configure<ChronicleAspNetCoreOptions>(options =>
        {
            options.EventStore = "test-store";
            options.EventStoreNamespaceResolverType = typeof(InvalidType);
        });
    }

    void Because()
    {
        _services.AddCratisChronicleClient();
        _serviceProvider = _services.BuildServiceProvider();
        _exception = Catch.Exception(() => _serviceProvider.GetRequiredService<IEventStoreNamespaceResolver>());
    }

    [Fact] void should_throw_invalid_operation_exception() => _exception.ShouldBeOfExactType<InvalidOperationException>();

    [Fact] void should_indicate_type_must_implement_interface() => _exception.Message.ShouldContain("must implement IEventStoreNamespaceResolver");

    class InvalidType;
}
