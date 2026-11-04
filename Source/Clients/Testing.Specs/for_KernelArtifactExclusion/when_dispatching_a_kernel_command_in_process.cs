// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Testing.Events;
using Microsoft.Extensions.DependencyInjection;
using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult;

namespace Cratis.Chronicle.Testing.for_KernelArtifactExclusion;

public class when_dispatching_a_kernel_command_in_process : given.an_application_host
{
    EventStoreForTesting _eventStore;
    ServiceProvider _provider;
    CommandResult _result;

    void Establish()
    {
        BuildHost(false);
        _eventStore = new EventStoreForTesting(null, Substitute.For<IClientArtifactsProvider>());
        var services = new ServiceCollection();
        services.AddCratisChronicleConnection();
        services.AddSingleton(_eventStore.Connection);
        _provider = services.BuildServiceProvider();
    }

    async Task Because() => _result = await _provider.GetRequiredService<IServices>().Users.SetInitialAdminPassword(new SetInitialAdminPasswordRequest
    {
        UserId = Guid.NewGuid(),
        Password = "a-test-password",
        ConfirmedPassword = "a-different-test-password"
    });

    [Fact] void should_keep_kernel_commands_out_of_the_consumer() => KernelCommandHandlersIn(_application.Services.GetRequiredService<ICommandHandlerProviders>().Handlers).ShouldBeEmpty();
    [Fact] void should_dispatch_to_the_kernel_password_confirmation_check() => _result.ValidationResults.Select(_ => _.Message).ShouldContain("Confirmed password must match the password.");
    [Fact] void should_identify_the_invalid_kernel_command_member() => _result.ValidationResults.Single(_ => _.Message == "Confirmed password must match the password.").Members.ShouldContainOnly("confirmedPassword");
    [Fact] void should_not_report_a_missing_handler_or_other_exception() => _result.HasExceptions.ShouldBeFalse();

    void Destroy()
    {
        _provider.Dispose();
        _eventStore.Connection.Dispose();
    }
}
