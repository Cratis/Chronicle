// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_releasing;

public class and_the_explicit_subject_has_no_value : given.a_recording_compliance_service
{
    record SubjectSignup(string Id, [property: Subject] Subject? UserId, [PII] string Email);

    SubjectSignup _result;

    async Task Because() => _result = await _readModels.Release(new SubjectSignup("signup-42", null, Cipher("signup-42", "user@example.com")));

    [Fact] void should_release_under_the_read_model_id() => _requests.Single().Subject.ShouldEqual("signup-42");
    [Fact] void should_return_the_released_value() => _result.Email.ShouldEqual("user@example.com");
}
