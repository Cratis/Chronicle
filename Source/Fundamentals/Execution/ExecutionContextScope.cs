// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents a temporary and disposable execution context scope.
/// </summary>
public class ExecutionContextScope : IDisposable
{
    readonly Action _onDispose;
    bool _isDisposed;

    /// <summary>
    /// Gets the <see cref="TenantId"/> of the scope.
    /// </summary>
    public TenantId TenantId { get; }

    /// <summary>
    /// Gets the <see cref="CorrelationId"/> of the scope.
    /// </summary>
    public CorrelationId CorrelationId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionContextScope"/> class.
    /// </summary>
    /// <param name="tenantId">The <see cref="TenantId"/> of the scope.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the scope.</param>
    /// <param name="onDispose">Action to call when the scope is disposed.</param>
    public ExecutionContextScope(TenantId tenantId, CorrelationId correlationId, Action onDispose)
    {
        TenantId = tenantId;
        CorrelationId = correlationId;
        _onDispose = onDispose;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed) return;

        _onDispose();

        _isDisposed = true;
    }
}
