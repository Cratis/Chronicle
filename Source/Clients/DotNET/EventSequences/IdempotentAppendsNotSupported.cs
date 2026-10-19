// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an idempotent append cannot be honored. The key was not sent and nothing was appended.
/// </summary>
public class IdempotentAppendsNotSupported : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentAppendsNotSupported"/> class for a kernel that lacks idempotent appends.
    /// </summary>
    public IdempotentAppendsNotSupported()
        : base("The Chronicle kernel does not support idempotent appends. The idempotency key was not sent and nothing was appended.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentAppendsNotSupported"/> class for an event sequence implementation that lacks idempotent appends.
    /// </summary>
    /// <param name="implementation">The implementation that cannot honor idempotency keys.</param>
    public IdempotentAppendsNotSupported(Type implementation)
        : base($"Event sequence implementation '{implementation.FullName}' does not support idempotent appends. The idempotency key was not sent and nothing was appended.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotentAppendsNotSupported"/> class for a context that cannot carry keys.
    /// </summary>
    /// <param name="context">A description of the context that cannot carry keys.</param>
    public IdempotentAppendsNotSupported(string context)
        : base($"Idempotency keys cannot be used {context}. The key was not sent and nothing was appended.")
    {
    }
}
