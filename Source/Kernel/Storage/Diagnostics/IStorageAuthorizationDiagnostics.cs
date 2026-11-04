// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Diagnostics;

/// <summary>
/// Tracks storage authorization failures for diagnostics and health reporting.
/// </summary>
public interface IStorageAuthorizationDiagnostics
{
    /// <summary>
    /// Gets a snapshot of the outstanding failures.
    /// </summary>
    IReadOnlyCollection<StorageAuthorizationFailure> Failures { get; }

    /// <summary>
    /// Records a failure and logs it once until the collection recovers.
    /// </summary>
    /// <param name="failure">The refused operation.</param>
    /// <returns>Whether this failure was newly recorded.</returns>
    bool Record(StorageAuthorizationFailure failure);

    /// <summary>
    /// Clears failures after the collection's index setup succeeds.
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <param name="collection">The collection name.</param>
    void Clear(string database, string collection);
}
