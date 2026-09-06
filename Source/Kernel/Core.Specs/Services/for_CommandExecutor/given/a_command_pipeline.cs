// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Services.for_CommandExecutor.given;

public class a_command_pipeline : Specification
{
    protected internal record TheCommand(string Name);

    protected ICommandPipeline _pipeline;
    protected TheCommand _command;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _pipeline = Substitute.For<ICommandPipeline>();
        _command = new TheCommand("Something");
        _correlationId = CorrelationId.New();
    }
}
