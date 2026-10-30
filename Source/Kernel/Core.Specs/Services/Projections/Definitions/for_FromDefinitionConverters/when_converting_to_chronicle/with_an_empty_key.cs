// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Projections.Definitions.for_FromDefinitionConverters.when_converting_to_chronicle;

public class with_an_empty_key : Specification
{
    Contracts.Projections.FromDefinition _contract;
    Concepts.Projections.Definitions.FromDefinition _result;

    void Establish() => _contract = new() { Key = "" };

    void Because() => _result = _contract.ToChronicle();

    [Fact] void should_leave_the_key_empty_for_the_engine_to_resolve() => ((string)_result.Key).ShouldEqual("");
}
