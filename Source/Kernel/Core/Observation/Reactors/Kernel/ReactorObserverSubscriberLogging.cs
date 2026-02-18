// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Log messages for ReactorObserverSubscriber.
/// </summary>
internal static partial class ReactorObserverSubscriberLogging
{
    [LoggerMessage(LogLevel.Debug, "ReactorObserverSubscriber for '{ReactorName}' activating")]
    internal static partial void ReactorObserverSubscriberActivating(this ILogger logger, string reactorName);

    [LoggerMessage(LogLevel.Debug, "ReactorObserverSubscriber for '{ReactorName}' activated successfully")]
    internal static partial void ReactorObserverSubscriberActivated(this ILogger logger, string reactorName);

    [LoggerMessage(LogLevel.Error, "Failed to activate ReactorObserverSubscriber for '{ReactorName}'")]
    internal static partial void FailedToActivateReactorObserverSubscriber(this ILogger logger, string reactorName, Exception exception);
}
