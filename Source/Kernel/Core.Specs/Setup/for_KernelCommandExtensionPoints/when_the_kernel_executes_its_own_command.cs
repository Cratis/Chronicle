// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints;

public class when_the_kernel_executes_its_own_command : given.a_host_shared_with_an_application
{
    Contracts.Commands.CommandResult _result;

    async Task Because()
    {
        given.ApplicationExtensionPoints.Activate(_recorder);
        _result = await CommandExecutor.Execute(_pipeline, new given.TheCommand());
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_be_refused_by_the_application_authorization() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_handle_the_command() => _recorder.Handled.ShouldBeTrue();
    [Fact] void should_not_begin_the_application_command_scope() => _recorder.ScopesBegun.ShouldEqual(0);
    [Fact] void should_not_run_the_application_command_filter() => _recorder.FiltersRun.ShouldEqual(0);
}
