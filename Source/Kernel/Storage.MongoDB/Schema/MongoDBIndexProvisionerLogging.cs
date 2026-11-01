// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Structured operational logging for MongoDB index maintenance.
/// </summary>
internal static partial class MongoDBIndexProvisionerLogging
{
    /// <summary>
    /// Logs aggregate maintenance counts without connection details.
    /// </summary>
    [LoggerMessage(LogLevel.Information, "MongoDB indexes for {Scope} ({Database}): {Missing} missing, {Obsolete} obsolete, {Incompatible} incompatible, {Created} created, {Dropped} dropped")]
    internal static partial void IndexState(this ILogger logger, string scope, string database, int missing, int obsolete, int incompatible, int created, int dropped);

    /// <summary>
    /// Warns about scopes requiring offline provisioning.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB indexes are not provisioned for {Scope} ({Database}): missing [{Missing}], obsolete [{Obsolete}]. Run --migrate-only with a schema identity.")]
    internal static partial void IndexesMissing(this ILogger logger, string scope, string database, string missing, string obsolete);

    /// <summary>
    /// Reports incompatible indexes only when any were found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scope">The logical scope.</param>
    /// <param name="database">The database name.</param>
    /// <param name="incompatible">The incompatible index names.</param>
    internal static void ReportIncompatibleIndexes(this ILogger logger, string scope, string database, IReadOnlyList<string> incompatible)
    {
        if (incompatible.Count > 0)
        {
            logger.IndexesIncompatible(scope, database, string.Join(", ", incompatible));
        }
    }

    /// <summary>
    /// Warns about existing indexes requiring operator review rather than automatic rebuilding.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB indexes for {Scope} ({Database}) do not match Chronicle's definitions: [{Incompatible}]. Chronicle does not rebuild existing indexes; drop them after review and run --migrate-only with a schema identity.")]
    internal static partial void IndexesIncompatible(this ILogger logger, string scope, string database, string incompatible);

    /// <summary>
    /// Warns about constraint collections skipped because their stored definitions cannot classify them.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB constraint collections are unclassified for {Scope} ({Database}): [{Collections}]. {Action}")]
    internal static partial void UnclassifiedConstraints(this ILogger logger, string scope, string database, string collections, string action);

    /// <summary>
    /// Reports a runtime constraint collection requiring privileged provisioning.
    /// </summary>
    [LoggerMessage(LogLevel.Error, "MongoDB constraint indexes are not provisioned for {Collection}, and this identity cannot create them. Provision the collection and its indexes with a schema identity before retrying.")]
    internal static partial void ConstraintIndexDenied(this ILogger logger, string collection, Exception exception);

    /// <summary>
    /// Reports a nonfatal startup inspection failure.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, "MongoDB startup index verification failed; startup will continue")]
    internal static partial void VerificationFailed(this ILogger logger, Exception exception);
}
