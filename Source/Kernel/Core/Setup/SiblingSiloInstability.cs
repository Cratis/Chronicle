// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using MongoDB.Driver;
using Orleans.Runtime.Messaging;

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Recognizes the failures a cross-silo grain call produces while a cluster is still forming.
/// </summary>
/// <remarks>
/// Every silo runs its own startup task, and those tasks make grain calls that can land on a sibling
/// silo. A sibling that is itself still starting - or that restarted a moment ago and has not been
/// reached yet - answers none of them, and the call fails after the default 30 second response
/// timeout. That is a transient state of a forming cluster, not a defect in the work being asked
/// for: the same call succeeds seconds later once membership settles.
/// <para>
/// A temporarily full Mongo connection wait queue is also retried within the same finite budget:
/// concurrent registration can saturate admission without indicating a permanent storage defect.
/// Everything else - a genuine bug in the grain or a serialization mismatch - fails immediately.
/// Exhausting the budget still fails startup; retries do not make an unavailable store healthy.
/// </para>
/// </remarks>
public static class SiblingSiloInstability
{
    /// <summary>
    /// The data key for a trusted silo's storage admission category.
    /// </summary>
    internal const string StorageAdmissionCategoryKey = "Cratis.Chronicle.StorageAdmission.Category";

    /// <summary>
    /// The category for an exact MongoDB wait-queue-full failure.
    /// </summary>
    internal const string MongoWaitQueueFullCategory = "MongoWaitQueueFull.v1";

    /// <summary>
    /// Determines whether an exception is a sibling silo that has not stabilized yet, rather than a
    /// failure of the work itself, or temporary saturation of storage admission.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to judge.</param>
    /// <returns><see langword="true"/> when the call is worth retrying.</returns>
    public static bool IsTransient(Exception exception) => exception switch
    {
        TimeoutException => true,
        SiloUnavailableException => true,
        OrleansMessageRejectionException => true,
        ConnectionFailedException => true,
        MongoWaitQueueFullException => true,
        _ => IsCategorizedStorageAdmissionFailure(exception)
    };

    static bool IsCategorizedStorageAdmissionFailure(Exception exception) =>
        exception?.GetType() == typeof(InvalidOperationException) &&
        exception.InnerException is null &&
        exception.Data.Cast<DictionaryEntry>().Any(entry =>
            entry.Key is string key &&
            entry.Value is string value &&
            string.Equals(key, StorageAdmissionCategoryKey, StringComparison.Ordinal) &&
            string.Equals(value, MongoWaitQueueFullCategory, StringComparison.Ordinal));
}
