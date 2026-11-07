// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_values_are_plain_or_null : given.a_factory
{
    ReleasedUniqueConstraintComponents _nullResult;

    void Establish()
    {
        SetContent("""{"name":"Ada","number":null}""");
        _definition = new("event", ["name", "number", "day"]);
        _statuses["name"] = ProtectedValueReleaseStatus.Plain;
        _statuses["number"] = ProtectedValueReleaseStatus.Plain;
    }

    async Task Because()
    {
        _result = await Create();
        _definition = new("event", ["number", "day"]);
        _nullResult = await Create();
    }

    [Fact] void should_report_readable_components() => _result.Outcome.ShouldEqual(UniqueConstraintReleaseOutcome.Claim);
    [Fact] void should_preserve_plain_text() => _result.Components[0].Value.ShouldEqual("Ada");
    [Fact] void should_not_default_literal_null_numbers() => _result.Components[1].Value.ShouldBeNull();
    [Fact] void should_not_default_missing_dates() => _result.Components[2].Value.ShouldBeNull();
    [Fact] void should_keep_null_distinct_from_erasure() => _nullResult.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_leave_all_null_claim_decisions_to_the_encoder() => UniqueConstraintEncoder.Encode(Encoding.ASCII.GetBytes("slice-2b-component-key"), _nullResult.Components).HasValue.ShouldBeFalse();
    [Fact] void should_not_mutate_source_content() => CurrentContent().ShouldEqual(_original);
}
