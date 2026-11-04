// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public sealed class HeldConstraintRebuildLogger
{
    public HeldConstraintRebuildLogger()
    {
        Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        Logger.When(logger => logger.Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>())).Do(call => Record(call.Args()));
    }

    public ILogger<EventSequence> Logger { get; } = Substitute.For<ILogger<EventSequence>>();

    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    void Record(object?[] arguments)
    {
        // Generated logging pools its state, so format it before the call returns.
        var message = (string)((Delegate)arguments[4]!).DynamicInvoke(arguments[2], arguments[3])!;
        Entries.Add(((LogLevel)arguments[0]!, message));
    }
}
