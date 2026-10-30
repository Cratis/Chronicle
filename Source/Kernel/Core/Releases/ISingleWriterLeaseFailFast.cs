// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Terminates a process that has outlived its safe single-writer deadline.
/// </summary>
public interface ISingleWriterLeaseFailFast
{
    /// <summary>
    /// Terminates immediately without running host shutdown handlers.
    /// </summary>
    /// <param name="message">The reason for termination.</param>
    void FailFast(string message);
}
