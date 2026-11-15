// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_applying;

public class and_serverless_is_on_with_an_invalid_configuration : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ServerlessActivation.Apply(new ChronicleOptions { Serverless = { Mode = ServerlessMode.Serverless } }, "Production"));

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<InvalidServerlessConfiguration>();
}
