// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Describes the outcome of releasing a protected value.
/// </summary>
public enum ProtectedValueReleaseStatus
{
    /// <summary>
    /// The value was not ciphertext and remains unchanged.
    /// </summary>
    Plain = 0,

    /// <summary>
    /// The ciphertext was successfully decrypted.
    /// </summary>
    Decrypted = 1,

    /// <summary>
    /// The ciphertext cannot be decrypted and its subject has a recorded erasure.
    /// </summary>
    Erased = 2
}
