// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessEnvironment.when_resolving;

public class and_dotnet_environment_is_set : Specification
{
    string _result;

    void Because() => _result = ServerlessEnvironment.Resolve(new DefaultServiceProvider(), name => name == "DOTNET_ENVIRONMENT" ? "Development" : "Staging");

    [Fact] void should_resolve_the_environment() => _result.ShouldEqual("Development");
}
