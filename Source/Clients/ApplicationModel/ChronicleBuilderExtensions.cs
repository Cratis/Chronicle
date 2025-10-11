// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.AspNetCore;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/> for adding ApplicationModel support.
/// </summary>
public static class ChronicleBuilderExtensions
{
    /// <summary>
    /// Adds ApplicationModel support to Chronicle configuration.
    /// </summary>
    /// <param name="builder">The <see cref="IChronicleBuilder"/> to add to.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder WithApplicationModel(this IChronicleBuilder builder)
    {
        builder.Services.AddAggregateRoots(builder.ClientArtifactsProvider);
        builder.Services.AddReadModels(builder.ClientArtifactsProvider);

        return builder;
    }
}
