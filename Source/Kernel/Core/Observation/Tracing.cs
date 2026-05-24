// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for tracing.
/// </summary>
internal static class Tracing
{
    /// <summary>
    /// Starts tracing for an observer handling events.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer.</param>
    /// <returns>The started <see cref="Activity"/>.</returns>
    public static Activity? Handle(ObserverKey observerKey)
    {
        var activity = ChronicleActivity.Source.StartActivity(nameof(Handle), ActivityKind.Internal);
        activity?.Tag(observerKey);
        return activity;
    }
}
