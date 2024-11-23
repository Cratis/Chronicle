// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Tasks;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.given;

public class all_dependencies : Specification
{
    protected ITaskFactory _taskFactory;
    protected IGrainFactory _grainFactory;

    void Establish()
    {
        _taskFactory = Substitute.For<ITaskFactory>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _taskFactory
            .When(_ => _.Run(Arg.Any<Func<Task>>()))
            .Do(callInfo => callInfo.Arg<Func<Task>>()());
    }
}
