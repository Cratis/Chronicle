// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// A declared value the query edge computed rather than read out of storage. The declaration routes it to a
/// subject that owns a key, and the release pass recognizes that the bytes are not something it produced and
/// hands them back. Declaring a subject must not turn a plaintext value into a blank one.
/// </summary>
public class and_the_declared_value_was_never_encrypted : given.a_recording_compliance_service
{
    record DueSubject(string PersonId, [PII][SubjectFrom(nameof(PersonId))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject("person-1", "Computed at the query edge"));

    [Fact] void should_release_under_the_declared_subject() => _requests.Single().Subject.ShouldEqual("person-1");
    [Fact] void should_return_the_value_untouched() => _result.Comment.ShouldEqual("Computed at the query edge");
}
