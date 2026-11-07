// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Serverless.for_ServerlessActivation.when_applying;

public class and_serverless_is_on_with_deploy_registration : Specification
{
    ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions { ProgramIdentifier = "orders", Serverless = { Mode = ServerlessMode.Serverless, DeploymentGeneration = 1 } };

    void Because() => ServerlessActivation.Apply(_options, "Production");

    [Fact] void should_skip_keep_alive() => _options.SkipKeepAlive.ShouldBeTrue();
    [Fact] void should_register_at_deployment() => _options.RegistrationMode.ShouldEqual(RegistrationMode.AtDeployment);
}
