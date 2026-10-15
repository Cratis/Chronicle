// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionExtensions.when_checking_equivalence;

public class definitions_with_different_indexes : given.definitions
{
    bool _result;

    void Because() => _result = Create().IsEquivalentTo(Create(index: "Other"));

    [Fact] void should_not_be_equivalent() => _result.ShouldEqual(false);
}
