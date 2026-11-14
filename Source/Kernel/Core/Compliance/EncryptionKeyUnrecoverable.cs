// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>A protected value could not be recovered using its permitted key revisions.</summary>
/// <param name="attemptedRevisions">The number of revisions tried.</param>
/// <param name="innerException">The cryptographic failure, without plaintext or key material.</param>
public class EncryptionKeyUnrecoverable(int attemptedRevisions, Exception innerException)
    : Exception("The protected value could not be recovered using the permitted key revisions.", innerException)
{
    /// <summary>Gets the number of revisions tried.</summary>
    public int AttemptedRevisions { get; } = attemptedRevisions;
}
