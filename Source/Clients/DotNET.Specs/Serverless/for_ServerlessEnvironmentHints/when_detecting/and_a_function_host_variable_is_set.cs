// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessEnvironmentHints.when_detecting;

public class and_a_function_host_variable_is_set : Specification
{
    string _result;

    void Because() => _result = ServerlessEnvironmentHints.Detect(name => name == "K_SERVICE" ? "orders" : null);

    [Fact] void should_name_the_variable() => _result.ShouldEqual("K_SERVICE");
}
