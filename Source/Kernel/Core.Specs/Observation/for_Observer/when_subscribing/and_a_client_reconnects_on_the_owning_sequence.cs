// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_client_reconnects_on_the_owning_sequence : given.an_observer_with_a_versioned_owner_on_another_sequence
{
    DefinitionOwnershipSwitch? _switch;

    async Task Establish()
    {
        await Register();
        _definitionStorageStats.ResetCounts();
    }

    async Task Because()
    {
        await Register();
        _switch = await (Task<DefinitionOwnershipSwitch?>)typeof(Observer)
            .GetMethod("ClaimDefinitionForKey", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(_observer, null)!;
    }

    [Fact] void should_not_report_another_move() => _switch.ShouldBeNull();
    [Fact] void should_only_write_the_definition_for_the_subscription() => _definitionStorageStats.Writes.ShouldEqual(1);
    [Fact] void should_not_recommend() => ShouldNotRecommend();
}
