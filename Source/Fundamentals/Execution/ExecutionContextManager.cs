// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Represents an implementation of <see cref="IExecutionContextManager"/>.
/// </summary>
[Singleton]
public class ExecutionContextManager : IExecutionContextManager
{
    static readonly AsyncLocal<ExecutionContext> _currentExecutionContext = new();

    /// <summary>
    /// Get whether or not we're inside the kernel.
    /// </summary>
    public static bool IsInKernel { get; private set; }

    /// <summary>
    /// Gets or sets a <see cref="ExecutionContextResolver"/> to use as a fallback to attempt to resolve execution context when it is not explicitly set.
    /// </summary>
    public static ExecutionContextResolver? Resolver { get; set; }

    /// <summary>
    /// Get the global <see cref="MicroserviceId"/> for the running process.
    /// </summary>
    /// <returns>The current microservice identifier.</returns>
    public static MicroserviceId GlobalMicroserviceId { get; private set; } = MicroserviceId.Unspecified;

    /// <inheritdoc/>
    public bool IsInContext => _currentExecutionContext?.Value != default;

    /// <inheritdoc/>
    public ExecutionContext Current => GetCurrent();

    /// <summary>
    /// Set a <see cref="ExecutionContext"/> for current call path.
    /// </summary>
    /// <param name="context"><see cref="ExecutionContext"/> to set.</param>
    public static void SetCurrent(ExecutionContext context) => _currentExecutionContext.Value = context;

    /// <summary>
    /// Get current <see cref="ExecutionContext"/>.
    /// </summary>
    /// <returns>Current <see cref="ExecutionContext"/>.</returns>
    public static ExecutionContext GetCurrent()
    {
        var current = _currentExecutionContext.Value;
        if (current is null && (Resolver?.Invoke(out current) ?? false))
        {
            _currentExecutionContext.Value = current;
        }
        ExecutionContextNotSet.ThrowIfNotSet(current!);

        return current!;
    }

    /// <summary>
    /// Set running process into kernel mode.
    /// </summary>
    public static void SetKernelMode() => IsInKernel = true;

    /// <summary>
    /// Set the global <see cref="MicroserviceId"/> for the running process.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to set.</param>
    /// <remarks>
    /// The global microservice id is the value being used when not a specific one is used
    /// while establishing a context for current task context.
    /// </remarks>
    public static void SetGlobalMicroserviceId(MicroserviceId microserviceId) => GlobalMicroserviceId = microserviceId;

    /// <inheritdoc/>
    public ExecutionContext Establish(MicroserviceId microserviceId)
    {
        _currentExecutionContext.Value = new ExecutionContext(
            microserviceId,
            TenantId.NotSet,
            CorrelationId.New(),
            string.Empty,
            Guid.Empty,
            IsInKernel);

        return _currentExecutionContext.Value;
    }

    /// <inheritdoc/>
    public ExecutionContext Establish(TenantId tenantId, CorrelationId correlationId, MicroserviceId? microserviceId = default)
    {
        _currentExecutionContext.Value = new ExecutionContext(
            microserviceId ?? GlobalMicroserviceId,
            tenantId,
            correlationId,
            string.Empty,
            Guid.Empty,
            IsInKernel);

        return _currentExecutionContext.Value;
    }

    /// <inheritdoc/>
    public void Set(ExecutionContext context) => _currentExecutionContext.Value = context;
}
