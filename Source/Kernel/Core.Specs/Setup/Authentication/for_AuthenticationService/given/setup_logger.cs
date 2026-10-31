// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Setup.Authentication.for_AuthenticationService.given;

public class setup_logger
{
    public setup_logger()
    {
        Logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        Logger.When(logger => logger.Log(Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<LoggerMessageState>(), Arg.Any<Exception?>(), Arg.Any<Func<LoggerMessageState, Exception?, string>>()))
            .Do(call =>
            {
                var formatter = call.ArgAt<Func<LoggerMessageState, Exception?, string>>(4);
                Messages.Add((call.ArgAt<LogLevel>(0), formatter(call.ArgAt<LoggerMessageState>(2), call.ArgAt<Exception?>(3))));
            });
    }

    public ILogger Logger { get; } = Substitute.For<ILogger>();
    public List<(LogLevel Level, string Message)> Messages { get; } = [];
}
