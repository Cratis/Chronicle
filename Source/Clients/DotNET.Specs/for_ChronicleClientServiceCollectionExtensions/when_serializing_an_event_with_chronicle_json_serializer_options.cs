// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

public class when_serializing_an_event_with_chronicle_json_serializer_options : given.a_host_adding_the_client
{
    record SomeEvent(string FirstName);

    ServiceProvider _provider;
    string _serialized;

    void Establish() => _chronicleOptions.JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    async Task Because()
    {
        _provider = BuildProvider();
        await using var scope = _provider.CreateAsyncScope();
        var serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();
        _serialized = (await serializer.Serialize(new SomeEvent("Jane"))).ToJsonString();
    }

    void Destroy() => _provider?.Dispose();

    [Fact] void should_use_the_naming_policy_of_the_chronicle_options() => _serialized.ShouldContain("first_name");
}
