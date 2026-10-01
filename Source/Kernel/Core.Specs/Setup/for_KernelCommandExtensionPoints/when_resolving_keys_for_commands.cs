// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Setup.for_KernelCommandExtensionPoints;

public class when_resolving_keys_for_commands : given.a_host_shared_with_an_application
{
    string? _applicationKeyBefore;
    string? _kernelKey;
    string? _applicationKeyAfter;

    void Because()
    {
        var keys = _provider.GetRequiredService<ICommandKeys>();
        var command = new given.TheCommand();
        given.ApplicationExtensionPoints.Activate(_recorder);

        _applicationKeyBefore = keys.GetKeyFor(command);
        using (KernelCommandExecution.Begin())
        {
            _kernelKey = keys.GetKeyFor(command);
        }

        _applicationKeyAfter = keys.GetKeyFor(command);
    }

    [Fact] void should_resolve_the_application_key_for_the_application_command() => _applicationKeyBefore.ShouldEqual(given.ApplicationCommandKeyResolver.Key);
    [Fact] void should_not_resolve_the_application_key_for_the_kernel_command() => _kernelKey.ShouldBeNull();
    [Fact] void should_resolve_the_application_key_for_the_application_command_again() => _applicationKeyAfter.ShouldEqual(given.ApplicationCommandKeyResolver.Key);
}
