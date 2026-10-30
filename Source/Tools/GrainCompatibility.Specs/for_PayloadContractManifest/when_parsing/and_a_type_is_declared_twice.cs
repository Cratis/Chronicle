// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_PayloadContractManifest.when_parsing;

public class and_a_type_is_declared_twice : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => PayloadContractManifest.Parse("json Cratis.Payload\njson Cratis.Payload"));

    [Fact] void should_reject_the_manifest() => _error.ShouldBeOfExactType<InvalidPayloadContractManifest>();
    [Fact] void should_identify_the_line() => _error.Message.ShouldContain("Line 2:");
}
