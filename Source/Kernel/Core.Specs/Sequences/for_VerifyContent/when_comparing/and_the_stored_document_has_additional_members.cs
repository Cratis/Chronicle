// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_the_stored_document_has_additional_members : given.a_stored_event
{
    void Establish() => _stored = _stored with { GenerationalContent = new Dictionary<int, string> { [1] = "{\"value\":42,\"subtypeValue\":true}" } };

    async Task Because() => _result = await Verify();

    [Fact] void should_not_discard_the_extra_content_before_comparing() => _result.Result.ShouldEqual(ContentVerificationResult.Different);
}
