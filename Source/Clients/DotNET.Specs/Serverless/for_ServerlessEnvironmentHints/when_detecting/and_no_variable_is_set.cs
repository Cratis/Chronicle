// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessEnvironmentHints.when_detecting;

public class and_no_variable_is_set : Specification
{
    string _result;

    void Because() => _result = ServerlessEnvironmentHints.Detect(_ => null);

    [Fact] void should_detect_nothing() => _result.ShouldBeNull();
}
