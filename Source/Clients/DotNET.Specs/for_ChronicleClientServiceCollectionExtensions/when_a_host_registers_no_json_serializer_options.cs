// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_a_host_registers_no_json_serializer_options : given.a_host_adding_the_client
{
    JsonSerializerOptions _resolved;
    ServiceProvider _provider;

    void Because()
    {
        _provider = BuildProvider();
        _resolved = _provider.GetService<JsonSerializerOptions>();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_not_register_json_serializer_options() => _resolved.ShouldBeNull();
    [Fact] void should_still_validate_all_registered_services() => Catch.Exception(() => _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }).Dispose()).ShouldBeNull();
}
