// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_an_application_policy_is_unknown : given.an_application_host
{
    Exception _error;

    void Establish() => BuildHost(false, registerApplicationPolicy: false);

    async Task Because() => _error = await Catch.Exception(() => _application.StartAsync());

    [Fact] void should_reject_the_unknown_application_policy() => _error.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_name_the_application_policy() => _error.Message.ShouldContain("Application.Required");
    [Fact] void should_not_report_a_kernel_policy() => _error.Message.ShouldNotContain("Chronicle.Administrator.Operations");
}
