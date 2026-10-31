// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_the_identity_was_renamed_to_empty : given.a_metadata_query
{
    void Establish() => _storedIdentities[_identityId] = _storedIdentities[_identityId] with { Name = string.Empty };

    async Task Because() => await Read(42);

    [Fact] void should_report_unavailable_name() => _result.Single().CausedBy.Resolution.ShouldEqual(IdentityResolution.NameUnavailable);
    [Fact] void should_not_fall_back_to_subject_for_the_name() => _result.Single().CausedBy.Name.ShouldBeNull();
    [Fact] void should_preserve_subject() => _result.Single().CausedBy.Subject.ShouldEqual("subject");
}
