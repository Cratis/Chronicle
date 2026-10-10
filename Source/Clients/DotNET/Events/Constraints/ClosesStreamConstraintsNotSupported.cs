// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when a constraint builder or connected kernel does not support closing events.
/// </summary>
public class ClosesStreamConstraintsNotSupported : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ClosesStreamConstraintsNotSupported"/> for an unsupported builder.
    /// </summary>
    public ClosesStreamConstraintsNotSupported() : base("This constraint builder implementation does not support closing-event constraints.")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ClosesStreamConstraintsNotSupported"/> with an unsupported-feature message.
    /// </summary>
    /// <param name="message">The unsupported-feature message.</param>
    public ClosesStreamConstraintsNotSupported(string message) : base(message)
    {
    }
}
