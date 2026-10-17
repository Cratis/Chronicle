// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Classifies provider failures for kernel repair, never for a wire response.
/// </summary>
public interface IStorageErrorClassifier
{
    /// <summary>
    /// Determines whether a failure, including any inner failure, may succeed on retry.
    /// </summary>
    /// <param name="exception">The storage failure.</param>
    /// <returns>Whether the failure is transient.</returns>
    bool IsTransient(Exception exception);
}
