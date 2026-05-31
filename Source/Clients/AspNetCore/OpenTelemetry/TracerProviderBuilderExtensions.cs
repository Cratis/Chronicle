// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using OpenTelemetry.Trace;

namespace Cratis.Chronicle.AspNetCore.OpenTelemetry;

/// <summary>
/// Extension methods for <see cref="TracerProviderBuilder"/> for Chronicle client instrumentation.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Add Chronicle client instrumentation to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add to.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> for continuation.</returns>
    public static TracerProviderBuilder AddCratisChronicleInstrumentation(this TracerProviderBuilder builder)
    {
        builder.AddSource(ClientActivity.SourceName);
        return builder;
    }
}
