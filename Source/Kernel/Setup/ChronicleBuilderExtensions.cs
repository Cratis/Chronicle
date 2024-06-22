// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for <see cref="IChronicleBuilder"/>.
/// </summary>
public static class ChronicleBuilderExtensions
{
    /// <summary>
    /// Configure services for the builder.
    /// </summary>
    /// <param name="builder"><see cref="IChronicleBuilder"/> to configure services for.</param>
    /// <param name="configureDelegate">Delegate for working with services.</param>
    /// <returns><see cref="IChronicleBuilder"/> for continuation.</returns>
    public static IChronicleBuilder ConfigureServices(this IChronicleBuilder builder, Action<IServiceCollection> configureDelegate)
    {
        configureDelegate(builder.Services);
        return builder;
    }
}
