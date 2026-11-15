// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_unrelated_values_are_erased : given.a_factory
{
    void Establish()
    {
        SetContent("""{"name":"","number":"opaque integer","identifier":"opaque guid","day":"opaque date","profile":"opaque object","names":"opaque array"}""");
        _event = _event with { Context = _event.Context with { Subject = Subject.NotSet } };
        _statuses["name"] = ProtectedValueReleaseStatus.Plain;
        _statuses["number"] = ProtectedValueReleaseStatus.Erased;
        _statuses["identifier"] = ProtectedValueReleaseStatus.Erased;
        _statuses["day"] = ProtectedValueReleaseStatus.Erased;
        _statuses["profile"] = ProtectedValueReleaseStatus.Erased;
        _statuses["names"] = ProtectedValueReleaseStatus.Erased;
    }

    async Task Because() => _result = await Create();

    [Fact] void should_fall_back_to_the_event_source() => _identifier.ShouldEqual("source");
    [Fact] void should_not_skip_a_readable_constraint() => _result.ShouldSkipClaimOrRemoval.ShouldBeFalse();
    [Fact] void should_not_convert_erased_siblings() => _result.Components.Single().Value.ShouldEqual("");
    [Fact] void should_keep_the_plaintext_schema_format() => _result.Components.Single().IsInSchema.ShouldBeTrue();
    [Fact] void should_leave_the_stored_content_unchanged() => CurrentContent().ShouldEqual(_original);
}
