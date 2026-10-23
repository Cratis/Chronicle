// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Testing.for_EventStoreForTesting.when_sharing_schemas;

public class and_binding_compensation : given.schema_sharing
{
    JsonSchema _firstLegacy;
    JsonSchema _secondLegacy;

    async Task Because()
    {
        _other = new(null, _artifacts);
        var first = Registry("first-alias");
        var second = Registry("second-alias");
        var schemas = await Task.WhenAll(
            Task.Run(() => _store.JsonSchemaGenerator.Generate(typeof(Compensation), first)),
            Task.Run(() => _other.JsonSchemaGenerator.Generate(typeof(Compensation), second))).WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _schema = schemas[0];
        _otherSchema = schemas[1];
        _firstLegacy = _store.JsonSchemaGenerator.GenerateLegacyEventType(typeof(Compensation), first);
        _secondLegacy = _other.JsonSchemaGenerator.GenerateLegacyEventType(typeof(Compensation), second);
    }

    [Fact] void should_bind_the_first_registry() => _schema.GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_bind_the_second_registry() => _otherSchema.GetCompensationFor().ShouldEqual("second-alias");
    [Fact] void should_bind_the_first_legacy_registry() => _firstLegacy.GetCompensationFor().ShouldEqual("first-alias");
    [Fact] void should_bind_the_second_legacy_registry() => _secondLegacy.GetCompensationFor().ShouldEqual("second-alias");

    static IEventTypes Registry(string alias)
    {
        var registry = Substitute.For<IEventTypes>();
        registry.HasFor(typeof(Original)).Returns(true);
        registry.GetEventTypeFor(typeof(Original)).Returns(new EventType(alias, 1));
        return registry;
    }

    public record Original(string Value);

    [CompensationFor<Original>]
    public record Compensation(string Value);
}
