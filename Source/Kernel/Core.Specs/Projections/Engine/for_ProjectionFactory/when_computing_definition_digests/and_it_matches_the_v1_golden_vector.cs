// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_computing_definition_digests;

public class and_it_matches_the_v1_golden_vector : given.a_definition
{
    void Because() => Compute();

    /// <summary>
    /// Changing this digest requires a new algorithm version.
    /// </summary>
    [Fact] void should_match_the_golden_digest() => _digest.Value.ShouldEqual("586A6433F33683683E2637A2188042544D1423788E9A8DDB1B2D0AECAA7C1488");
}
