// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_a_newer_full_set_supersedes_an_unapplied_one : given.an_inbox
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<string> _applied = [];
    RegistrationAcceptance _first;
    RegistrationAcceptance _second;
    RegistrationAcceptance _third;
    RegistrationStatus _status;

    protected override void PrepareBeforeActivation() => _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(call =>
    {
        _applied.Add(call.ArgAt<IEnumerable<ProjectionDefinition>>(1).Single().Identifier.Value);
        return _applied.Count == 1 ? _release.Task : Task.CompletedTask;
    });

    async Task Because()
    {
        _first = await Accept([CreateDefinition("first")], "studio");
        await WaitUntil(() => _applied.Count == 1);

        // The first is being applied; the second waits behind it and the third replaces the second.
        _second = await Accept([CreateDefinition("second")], "studio");
        _third = await Accept([CreateDefinition("third")], "studio");
        _release.SetResult();
        await WaitUntil(() => _applied.Count == 2 && _intents.Intents.All(intent => intent.State == RegistrationIntentState.Applied));
        _status = await _grain.GetStatus(RegistrationKind.Projections, "studio");
    }

    [Fact] void should_number_the_registrations_in_the_order_they_were_accepted() => (_first.Revision < _second.Revision && _second.Revision < _third.Revision).ShouldBeTrue();
    [Fact] void should_apply_the_one_in_flight_and_the_newest() => _applied.ShouldEqual(["first", "third"]);
    [Fact] void should_never_apply_the_superseded_one() => _applied.ShouldNotContain("second");
    [Fact] void should_keep_one_intent_for_the_registrant() => _intents.Intents.Count.ShouldEqual(1);
    [Fact] void should_report_the_newest_revision_as_applied() => _status.AppliedRevision.ShouldEqual(_third.Revision);
}
