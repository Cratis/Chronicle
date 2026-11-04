// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.and_the_revision_changes_a_per_value_unique_claim.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

[Collection(ChronicleCollection.Name)]
public class and_the_revision_changes_a_per_value_unique_claim(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_event_log_with_revised_unique_claims(fixture)
    {
        Task Because() => ReviseAndProbe(nameof(UniqueAssignedCode), code => new CodeAssigned(code));
    }

    [Fact] void should_accept_the_original_claim() => Context.OriginalAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_only_the_original_owners_claim_before_revision() => Context.ClaimsBeforeRevision.Single().StartsWith($"{Context.Owner.Value}:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_record_the_original_claims_position() => Context.ClaimsBeforeRevision.Single().EndsWith($"@{Context.SequenceNumber.Value}", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_release_the_old_value() => Context.OldValueAppend.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_the_new_value_for_another_source() => Context.NewValueAppend.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_report_errors_for_the_rejected_claim() => Context.NewValueAppend.Errors.ShouldBeEmpty();
    [Fact] void should_keep_the_two_accepted_claims() => Context.ClaimsAfterRevision.Count.ShouldEqual(2);
    [Fact] void should_match_a_full_reindex() => Context.ClaimsAfterReindex.ShouldContainOnly(Context.ClaimsAfterRevision);
}
