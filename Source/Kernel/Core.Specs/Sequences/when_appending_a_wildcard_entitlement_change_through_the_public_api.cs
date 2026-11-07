// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences;

public class when_appending_a_wildcard_entitlement_change_through_the_public_api : Specification
{
    const string Reserved = "The System event store is reserved for the kernel and cannot be written through the public API.";
    const string WildcardContent = "{\"holder\":{\"issuer\":\"chronicle\",\"subject\":\"attacker\",\"isClient\":true},\"access\":7,\"source\":0}";

    public static TheoryData<Type, string> EntitlementEventTypes() => new()
    {
        { typeof(EventStoreAccessGranted), "System" },
        { typeof(EventStoreAccessGranted), "system" },
        { typeof(EventStoreAccessRevoked), "System" },
        { typeof(EventStoreAccessRevoked), "SYSTEM" }
    };

    [Theory]
    [MemberData(nameof(EntitlementEventTypes))]
    public async Task should_refuse_append(Type eventType, string store)
    {
        var result = await Scenario<Append>().Validate(new Append(store, "Default", "event-log", "store-entitlements", "Default", "All", "Default", ApiType(eventType), WildcardContent));
        ShouldBeRefused(result);
    }

    [Theory]
    [MemberData(nameof(EntitlementEventTypes))]
    public async Task should_refuse_append_many(Type eventType, string store)
    {
        var result = await Scenario<AppendMany>().Validate(new AppendMany(store, "Default", "event-log", "store-entitlements", [new EventToAppend(ApiType(eventType), WildcardContent)]));
        ShouldBeRefused(result);
    }

    static EventType ApiType(Type eventType) => new(eventType.GetEventType().Id.Value, 1, false);

    static CommandScenario<TCommand> Scenario<TCommand>()
    {
        var scenario = ChronicleCommandScenario.For<TCommand>();
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        scenario.Services.AddSingleton(storage);
        return scenario;
    }

    static void ShouldBeRefused(CommandResult result)
    {
        result.IsValid.ShouldBeFalse();
        result.ValidationResults.Select(_ => _.Message).ShouldContain(Reserved);
    }
}
