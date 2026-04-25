// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Wraps an <see cref="IServiceScope"/> so that its <see cref="IServiceProvider"/>
/// is a <see cref="FallbackServiceProvider"/>.
/// </summary>
/// <param name="inner">The real <see cref="IServiceScope"/>.</param>
/// <param name="registry">The <see cref="MutableServiceRegistry"/> to fall back to.</param>
internal sealed class FallbackServiceScope(IServiceScope inner, MutableServiceRegistry registry) : IServiceScope
{
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; } = new FallbackServiceProvider(inner.ServiceProvider, registry);

    /// <inheritdoc/>
    public void Dispose() => inner.Dispose();
}
