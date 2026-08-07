// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The reported no-op, pinned as it stands. A read model with no <c>[Subject]</c>, no <c>Id</c> and no
/// declaration still resolves no subject, still issues no call, and still hands back what it was given —
/// which is correct for a value that was never encrypted, and is the shape Chronicle documents for a
/// computed <c>[PII]</c> record. Per-property declarations must not change it; the declaration is how a read
/// model opts out of it.
/// </summary>
public class and_no_subject_resolves_and_nothing_is_declared : given.a_recording_compliance_service
{
    record DueSubject(string SubjectId, [PII] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject("person-1", Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_not_release() => _requests.ShouldBeEmpty();
    [Fact] void should_return_the_value_as_it_was_read() => _result.Comment.ShouldEqual(Cipher("person-1", "Awaiting counsel"));
}
