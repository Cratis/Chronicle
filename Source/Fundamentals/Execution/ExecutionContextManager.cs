// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution
{
    /// <summary>
    /// Represents an implementation of <see cref="IExecutionContextManager"/>.
    /// </summary>
    [Singleton]
    public class ExecutionContextManager : IExecutionContextManager
    {
        static readonly AsyncLocal<ExecutionContext> _currentExecutionContext = new();

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
            var current = _currentExecutionContext?.Value;
            ExecutionContextNotSet.ThrowIfNotSet(current!);

            return current!;
        }

        /// <inheritdoc/>
        public ExecutionContext Establish(TenantId tenantId, CorrelationId correlationId)
        {
            _currentExecutionContext.Value = new ExecutionContext(
                tenantId,
                correlationId,
                string.Empty,
                Guid.Empty);

            return _currentExecutionContext.Value;
        }

        /// <inheritdoc/>
        public void Set(ExecutionContext context) => _currentExecutionContext.Value = context;
    }
}
