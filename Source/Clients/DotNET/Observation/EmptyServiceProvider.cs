// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an <see cref="IServiceProvider"/> that provides nothing, for invoking without a service scope.
/// </summary>
internal sealed class EmptyServiceProvider : IServiceProvider
{
    /// <summary>
    /// Gets the single instance.
    /// </summary>
    public static readonly EmptyServiceProvider Instance = new();

    /// <inheritdoc/>
    public object? GetService(Type serviceType) => null;
}
