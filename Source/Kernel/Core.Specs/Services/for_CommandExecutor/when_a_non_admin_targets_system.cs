// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_CommandExecutor;

public class when_a_non_admin_targets_system : global::Cratis.Chronicle.Security.given.a_system_store_caller
{
    ICommandPipeline _pipeline;
    RetryPartition _command;
    Contracts.Commands.CommandResult _result;

    void Establish()
    {
        _caller = CallerNamed(OrdinaryUser);
        _command = new("System", "Default", "observer", "event-log", "partition");
        _pipeline = Substitute.For<ICommandPipeline>();

        // Run the command through the guard filter the way the pipeline does, and hand its verdict to the transport.
        _pipeline.Execute(_command, Arg.Any<Arc.Validation.ValidationResultSeverity?>()).Returns(async _ =>
        {
            var context = new CommandContext(
                CorrelationId.New(), _command.GetType(), _command, [], new CommandContextValues(), ServiceProvider: CreateServices());
            return await new SystemStoreAuthorizationCommandFilter().OnExecution(context);
        });
    }

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, _command);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_the_refusal() => _result.AuthorizationFailureReason.ShouldEqual(SystemStoreAccess.Refused);
}
