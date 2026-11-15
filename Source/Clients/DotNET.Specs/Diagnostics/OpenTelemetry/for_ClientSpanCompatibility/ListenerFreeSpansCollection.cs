// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.for_ClientSpanCompatibility;

/// <summary>
/// Isolates listener-free checks from specs that install process-wide listeners.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public static class ListenerFreeSpansCollection
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public const string Name = "Listener-free client spans";
}
