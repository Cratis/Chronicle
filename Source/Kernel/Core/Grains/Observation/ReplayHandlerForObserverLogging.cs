// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Logging for <see cref="ICanHandleReplayForObserver"/>.
/// </summary>
internal static partial class ReplayHandlerForObserverLogging
{
    [LoggerMessage(LogLevel.Warning, "An unknown error occurred for observer of type {ObserverType} and id {ObserverId}")]
    internal static partial void Failed(this ILogger<ICanHandleReplayForObserver> logger, Exception ex, ObserverId observerId, ObserverType observerType);
}
