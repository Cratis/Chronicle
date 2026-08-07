// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

/// <summary>
/// The regression guard for every read model that existed before per-property declarations: one call, the
/// whole payload, the read model's own subject. Nothing about this path may move.
/// </summary>
public class and_the_read_model_declares_nothing : given.a_recording_compliance_service
{
    record Employee(string Id, [PII] string Name, string Department);

    Employee _result;

    async Task Because() => _result = await _readModels.Release(new Employee("emp-1", Cipher("emp-1", "Ada Lovelace"), "Analytical Engines"));

    [Fact] void should_release_once() => _requests.Count.ShouldEqual(1);
    [Fact] void should_release_under_the_read_models_own_subject() => _requests[0].Subject.ShouldEqual("emp-1");
    [Fact] void should_send_the_whole_payload() => PayloadKeysFor("emp-1").ShouldContainOnly([nameof(Employee.Id), nameof(Employee.Name), nameof(Employee.Department)]);
    [Fact] void should_return_the_released_value() => _result.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_leave_the_non_personal_value_alone() => _result.Department.ShouldEqual("Analytical Engines");
}
