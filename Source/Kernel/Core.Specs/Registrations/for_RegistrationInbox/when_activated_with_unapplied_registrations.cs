// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox;

public class when_activated_with_unapplied_registrations : given.an_inbox
{
    readonly List<string> _applied = [];
    RegistrationStatus _status;
    RegistrationAcceptance _next;

    protected override void PrepareBeforeActivation()
    {
        // What a kernel that stopped before it applied the registration left behind, and one it had applied.
        ProjectionDefinition[] unapplied = [CreateDefinition("unapplied")];
        _intents.Seed(new(RegistrationKind.Projections, "studio", "studio", 7, ProjectionRegistrationSignature.For(unapplied, ProjectionOwner.Client, "studio", "1.0"), RegistrationIntentState.Pending, _payloads.Serialize(unapplied), (int)ProjectionOwner.Client, "1.0", 0, null, DateTimeOffset.UnixEpoch));
        _intents.Seed(new(RegistrationKind.Projections, "other", "other", 3, "hash", RegistrationIntentState.Applied, ReadOnlyMemory<byte>.Empty, null, null, 0, null, DateTimeOffset.UnixEpoch));
        _manager.RegisterShared(default!, default!, default, default, default, default).ReturnsForAnyArgs(call =>
        {
            _applied.Add(call.ArgAt<IEnumerable<ProjectionDefinition>>(1).Single().Identifier.Value);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        await WaitUntil(() => _applied.Count == 1);
        await WaitUntil(() => _intents.Intents.All(intent => intent.State == RegistrationIntentState.Applied));
        _status = await _grain.GetStatus(RegistrationKind.Projections, "studio");
        _next = await Accept([CreateDefinition("newer")], "studio");
    }

    [Fact] void should_apply_what_was_left_unapplied() => _applied.ShouldContain("unapplied");
    [Fact] void should_apply_only_what_was_unapplied() => _applied.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_report_it_as_applied() => _status.State.ShouldEqual(RegistrationIntentState.Applied);
    [Fact] void should_keep_the_applied_revision() => _status.AppliedRevision.ShouldEqual(7);
    [Fact] void should_number_a_new_registration_after_every_revision_it_has_seen() => _next.Revision.ShouldEqual(8);
}
