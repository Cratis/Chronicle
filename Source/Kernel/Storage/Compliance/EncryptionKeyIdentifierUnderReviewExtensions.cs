// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Extension methods for surfacing an <see cref="EncryptionKeyIdentifierUnderReview"/> refusal.
/// </summary>
public static class EncryptionKeyIdentifierUnderReviewExtensions
{
    /// <summary>
    /// Throws the first <see cref="EncryptionKeyIdentifierUnderReview"/> among the failures, if there is one.
    /// </summary>
    /// <remarks>
    /// The refusal never names the identifier, so it is raised as it is instead of being wrapped by an exception whose
    /// message does.
    /// </remarks>
    /// <param name="failures">The failures reported by the stores.</param>
    public static void ThrowIfUnderReview(this IEnumerable<Exception> failures)
    {
        if (failures.OfType<EncryptionKeyIdentifierUnderReview>().FirstOrDefault() is { } refusal)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(refusal).Throw();
        }
    }
}
