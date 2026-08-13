// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The declaration resolves to a property, but that property holds no subject on this row. There is nothing
/// to release under, and releasing under the row's own subject instead would be the wrong-subject outcome
/// the declaration was written to prevent. The value is left as read — and, unlike every outcome the report
/// describes, it is written to the log rather than swallowed. A single unreadable value never fails a query.
/// </summary>
public class and_the_declared_subject_property_holds_nothing : given.a_recording_compliance_service
{
    record DueSubject(string Id, string PersonId, [PII][SubjectFrom(nameof(PersonId))] string Comment);

    DueSubject _result;

    async Task Because() => _result = await _readModels.Release(new DueSubject("case-9", string.Empty, Cipher("person-1", "Awaiting counsel")));

    [Fact] void should_not_release_the_declared_property() => _requests.ShouldNotContain(request => request.Payload.Contains(nameof(DueSubject.Comment), StringComparison.Ordinal));
    [Fact] void should_still_release_everything_else_under_the_rows_own_subject() => PayloadKeysFor("case-9").ShouldContainOnly([nameof(DueSubject.Id), nameof(DueSubject.PersonId)]);
    [Fact] void should_return_the_declared_value_as_it_was_read() => _result.Comment.ShouldEqual(Cipher("person-1", "Awaiting counsel"));
}
