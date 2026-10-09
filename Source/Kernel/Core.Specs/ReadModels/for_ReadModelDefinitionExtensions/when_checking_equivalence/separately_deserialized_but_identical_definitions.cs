// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelDefinitionExtensions.when_checking_equivalence;

public class separately_deserialized_but_identical_definitions : given.definitions
{
    bool _result;

    void Because() => _result = Create().IsEquivalentTo(Create());

    [Fact] void should_be_equivalent() => _result.ShouldEqual(true);
}
