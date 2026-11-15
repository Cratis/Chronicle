// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Selects the instrument names and attribute keys published by the kernel.
/// </summary>
public enum MetricNames
{
    /// <summary>Publishes the existing names. This is the default until the next major release.</summary>
    Legacy = 0,

    /// <summary>Publishes only the shared telemetry convention names.</summary>
    Convention = 1,

    /// <summary>Publishes both sets of names for an opt-in dashboard migration.</summary>
    Both = 2
}
