// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_CommandDefinition.when_resolving_the_response_type;

public class and_handle_returns_a_validation_result : Specification
{
    Type? _responseType;

    void Because() => _responseType = new CommandDefinition(typeof(TestAssembly.Catalog.RejectProduct)).ResponseType;

    [Fact] void should_produce_no_response() => _responseType.ShouldBeNull();
}
