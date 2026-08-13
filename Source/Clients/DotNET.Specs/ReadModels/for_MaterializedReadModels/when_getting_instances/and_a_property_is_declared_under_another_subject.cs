// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModels.when_getting_instances;

/// <summary>
/// A declaration means the same thing on every read surface. An attribute that is honored through
/// <c>IReadModels.Release</c> and ignored through the materialized surface would be worse than not having
/// it, because which one a caller reaches through is not something the read model can see.
/// </summary>
public class and_a_property_is_declared_under_another_subject : given.a_recording_compliance_service
{
    record Assignment(string Id, string PersonId, [PII][SubjectFrom(nameof(PersonId))] string Name);

    IEnumerable<Assignment> _result = [];

    void Establish() => StoredInstance<Assignment>($$"""{"Id":"case-9","PersonId":"person-1","Name":"{{Cipher("person-1", "Ada Lovelace")}}"}""");

    async Task Because() => _result = await _readModels.Materialized.GetInstances<Assignment>();

    [Fact] void should_release_under_the_declared_subject() => _requests.Select(_ => _.Subject).ShouldContain("person-1");
    [Fact] void should_return_the_released_value() => _result.Single().Name.ShouldEqual("Ada Lovelace");
}
