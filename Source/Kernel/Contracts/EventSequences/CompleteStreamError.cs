// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

#pragma warning disable CA1008

/// <summary>
/// Wire-level representation of the errors that can occur when completing a stream.
/// </summary>
public enum CompleteStreamError
{
    /// <summary>
    /// The stream was already completed previously.
    /// </summary>
    AlreadyCompleted = 0,

    /// <summary>
    /// The default stream cannot be completed.
    /// </summary>
    DefaultStreamCannotBeCompleted = 1,
}
