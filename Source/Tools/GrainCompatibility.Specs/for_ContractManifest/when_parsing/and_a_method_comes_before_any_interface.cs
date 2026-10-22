// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_ContractManifest.when_parsing;

public class and_a_method_comes_before_any_interface : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ContractManifest.Parse("  E36EB1EC: Ensure() -> Task"));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<InvalidContractManifest>();
}
