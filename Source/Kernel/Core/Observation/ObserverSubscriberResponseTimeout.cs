// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Holds the Orleans response timeout that bounds a call handing events to an observer's subscriber.
/// </summary>
/// <remarks>
/// A subscriber call is a grain call, and without its own response timeout Orleans fails it after the silo-wide
/// <c language="csharp">clustering.responseTimeout</c> (30 seconds by default). That made
/// <c language="csharp">observers.subscriberTimeout</c> unable to wait any longer than that. The subscriber methods carry this
/// ceiling instead, so the configured subscriber timeout is the deadline that fires. It stays below Orleans'
/// <c language="csharp">MaxRequestProcessingTime</c> (two hours by default), after which an activation is considered stuck.
/// </remarks>
public static class ObserverSubscriberResponseTimeout
{
    /// <summary>
    /// The ceiling as the <see cref="TimeSpan"/> string the Orleans <c language="csharp">ResponseTimeout</c> attribute takes.
    /// </summary>
    public const string Value = "01:00:00";

    /// <summary>
    /// Gets the ceiling as a <see cref="TimeSpan"/>.
    /// </summary>
    public static readonly TimeSpan Ceiling = TimeSpan.Parse(Value, System.Globalization.CultureInfo.InvariantCulture);
}
