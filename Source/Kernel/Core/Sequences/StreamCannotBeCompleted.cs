// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// The exception that is thrown when a stream cannot be completed.
/// </summary>
/// <param name="error">The reason the stream could not be completed.</param>
public class StreamCannotBeCompleted(CompleteStreamError error) : Exception($"The stream cannot be completed: {error}")
{
    /// <summary>
    /// Gets the reason the stream could not be completed.
    /// </summary>
    public CompleteStreamError Error => error;
}
