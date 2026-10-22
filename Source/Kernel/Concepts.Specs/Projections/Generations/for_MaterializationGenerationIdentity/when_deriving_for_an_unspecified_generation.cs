// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationGenerationIdentity;

public class when_deriving_for_an_unspecified_generation : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => MaterializationGenerationIdentity.For("orders", MaterializationGeneration.Unspecified, "Order"));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidMaterializationGeneration>();
}
