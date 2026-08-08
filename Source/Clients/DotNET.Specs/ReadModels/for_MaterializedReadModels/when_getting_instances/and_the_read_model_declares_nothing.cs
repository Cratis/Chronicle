// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModels.when_getting_instances;

/// <summary>
/// The materialized surface runs the same release pass as every other read surface. Before the pass was one
/// collaborator this was a second, independently maintained copy of it, and nothing specified it at all.
/// </summary>
public class and_the_read_model_declares_nothing : given.a_recording_compliance_service
{
    record Employee(string Id, [property: PII] string Name);

    IEnumerable<Employee> _result = [];

    void Establish() => StoredInstance<Employee>($$"""{"Id":"emp-1","Name":"{{Cipher("emp-1", "Ada Lovelace")}}"}""");

    async Task Because() => _result = await _readModels.Materialized.GetInstances<Employee>();

    [Fact] void should_release_once() => _requests.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_read_models_own_subject() => _requests[0].Subject.ShouldEqual("emp-1");
    [Fact] void should_return_the_released_value() => _result.Single().Name.ShouldEqual("Ada Lovelace");
}
