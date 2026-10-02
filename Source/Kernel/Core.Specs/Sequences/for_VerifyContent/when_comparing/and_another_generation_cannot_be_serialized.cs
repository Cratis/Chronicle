// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_another_generation_cannot_be_serialized : given.a_migrated_event
{
    void Establish() => _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
        .SerializeContentForVerification(Arg.Any<ExpandoObject>(), _definition.Generations.Last().Schema).Returns((string?)null);

    async Task Because() => _result = await Verify();

    [Fact] void should_not_compare_only_the_requested_generation() => _result.Result.ShouldEqual(ContentVerificationResult.Unavailable);
}
