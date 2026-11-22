// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_registrations_are_not_a_full_set : given.an_inbox
{
    readonly List<string> _applied = [];

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(call =>
    {
        _applied.Add(call.ArgAt<IEnumerable<ProjectionDefinition>>(1).Single().Identifier.Value);
        return Task.CompletedTask;
    });

    async Task Because()
    {
        await Accept([CreateDefinition("one")], "studio", fullSetOwner: null);
        await Accept([CreateDefinition("two")], "studio", fullSetOwner: null);
        await WaitUntil(() => _applied.Count == 2);
    }

    [Fact] void should_keep_each_partial_registration() => _intents.Intents.Count.ShouldEqual(2);
    [Fact] void should_apply_each_of_them() => _applied.OrderBy(_ => _).ShouldEqual(["one", "two"]);
}
