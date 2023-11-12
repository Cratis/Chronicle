// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Conventions.for_ConventionSignature.when_checking_if_matches.async;

public class and_method_with_one_parameter_that_should_not_match_signature : Specification
{
    delegate Task AsyncMethodWithOneParameter(string input);

    class Methods
    {
        public Task MatchingMethod(string input, int something) => Task.CompletedTask;
    }

    ConventionSignature signature;
    bool result;

    void Establish() => signature = new(typeof(AsyncMethodWithOneParameter));

    void Because() => result = signature.Matches(typeof(Methods).GetMethod(nameof(Methods.MatchingMethod))!);

    [Fact] void should_not_match() => result.ShouldBeFalse();
}
