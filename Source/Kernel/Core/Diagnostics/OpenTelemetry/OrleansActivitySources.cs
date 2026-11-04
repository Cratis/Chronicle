// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Orleans activity sources used for tracing and context propagation.
/// </summary>
public static class OrleansActivitySources
{
    /// <summary>
    /// The Orleans runtime activity source.
    /// </summary>
    public const string Runtime = "Microsoft.Orleans.Runtime";

    /// <summary>
    /// The Orleans application activity source.
    /// </summary>
    public const string Application = "Microsoft.Orleans.Application";

    /// <summary>
    /// All Orleans activity sources.
    /// </summary>
    public static readonly string[] All = [Runtime, Application];
}
