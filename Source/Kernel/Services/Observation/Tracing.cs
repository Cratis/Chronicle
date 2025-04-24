// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Extension methods for tracing.
/// </summary>
internal static class Tracing
{
    /// <summary>
    /// Starts tracing for registering an observer.
    /// </summary>
    /// <param name="key">The <see cref="ConnectedObserverKey"/>.</param>
    /// <param name="observerType">The <see cref="ObserverType"/>.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? RegisterObserver(ConnectedObserverKey key, ObserverType observerType)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(RegisterObserver), ActivityKind.Server);
        activity?.Tag(key, observerType);
        return activity;
    }
}
