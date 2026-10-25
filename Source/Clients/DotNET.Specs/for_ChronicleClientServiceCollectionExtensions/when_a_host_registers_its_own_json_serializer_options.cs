// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_a_host_registers_its_own_json_serializer_options : given.a_host_adding_the_client
{
    record SomeEvent(string FirstName);

    JsonSerializerOptions _hostOptions;
    JsonSerializerOptions _resolved;
    ServiceProvider _provider;
    string _serialized;

    void Establish()
    {
        _hostOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _chronicleOptions.JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        _services.AddSingleton(_hostOptions);
    }

    async Task Because()
    {
        _provider = BuildProvider();
        _resolved = _provider.GetRequiredService<JsonSerializerOptions>();
        using var scope = _provider.CreateScope();
        var serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
        _serialized = (await serializer.Serialize(new SomeEvent("Jane"))).ToJsonString();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_keep_the_options_of_the_host() => _resolved.ShouldBeSame(_hostOptions);
    [Fact] void should_serialize_with_the_options_of_chronicle() => _serialized.ShouldContain("first_name");
}
