// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_terminal_proof_only_covers_older_wakes : given.an_owner
{
    string[] _remaining;

    async Task Establish()
    {
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow()) with { Revision = 10 };
        await _owner.RegisterOrUpdateReminder("namespace-incarnation-wake:10", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        await _owner.RegisterOrUpdateReminder("namespace-incarnation-wake:11", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        await _owner.RegisterOrUpdateReminder("unrelated", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    async Task Because()
    {
        await _owner.Wake();
        _remaining = (await _owner.GetReminders()).Select(_ => _.ReminderName).Order().ToArray();
    }

    [Fact] void should_clear_only_wakes_covered_by_the_observed_revision() => _remaining.ShouldEqual(["namespace-incarnation-wake:11", "unrelated"]);
    [Fact] void should_not_change_the_control_state() => _writes.ShouldBeEmpty();
}
