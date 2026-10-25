// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_a_host_registers_its_own_json_serializer_options : given.a_host_adding_the_client
{
    JsonSerializerOptions _hostOptions;
    JsonSerializerOptions _resolved;
    ServiceProvider _provider;

    void Establish()
    {
        _hostOptions = new JsonSerializerOptions();
        _services.AddSingleton(_hostOptions);
    }

    void Because()
    {
        _provider = BuildProvider();
        _resolved = _provider.GetRequiredService<JsonSerializerOptions>();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_keep_the_options_of_the_host() => _resolved.ShouldBeSame(_hostOptions);
}
