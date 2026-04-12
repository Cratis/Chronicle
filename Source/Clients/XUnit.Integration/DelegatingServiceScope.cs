// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a scope created by <see cref="DelegatingServiceProvider"/>.
/// </summary>
/// <param name="rootScope">The root scope.</param>
/// <param name="currentScope">The current test scope.</param>
internal class DelegatingServiceScope(IServiceScope? rootScope, IServiceScope? currentScope) : IServiceScope, IAsyncDisposable
{
    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; } = new ScopedDelegatingServiceProvider(rootScope?.ServiceProvider, currentScope?.ServiceProvider);

    /// <inheritdoc/>
    public void Dispose()
    {
        currentScope?.Dispose();
        rootScope?.Dispose();
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        currentScope?.Dispose();
        rootScope?.Dispose();
        return ValueTask.CompletedTask;
    }
}
