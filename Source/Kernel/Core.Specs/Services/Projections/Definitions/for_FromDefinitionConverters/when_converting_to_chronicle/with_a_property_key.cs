// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.Definitions.for_FromDefinitionConverters.when_converting_to_chronicle;

public class with_a_property_key : Specification
{
    Contracts.Projections.FromDefinition _contract;
    Concepts.Projections.Definitions.FromDefinition _result;

    void Establish() => _contract = new() { Key = "orderId" };

    void Because() => _result = _contract.ToChronicle();

    [Fact] void should_keep_the_key() => ((string)_result.Key).ShouldEqual("orderId");
}
