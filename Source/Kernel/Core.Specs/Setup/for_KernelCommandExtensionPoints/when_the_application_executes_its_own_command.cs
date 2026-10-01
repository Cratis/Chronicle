// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints;

public class when_the_application_executes_its_own_command : given.a_host_shared_with_an_application
{
    CommandResult _result;

    async Task Because()
    {
        given.ApplicationExtensionPoints.Activate(_recorder);
        _result = await _pipeline.Execute(new given.TheCommand());
    }

    [Fact] void should_be_refused_by_the_application_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_handle_the_command() => _recorder.Handled.ShouldBeFalse();
}
