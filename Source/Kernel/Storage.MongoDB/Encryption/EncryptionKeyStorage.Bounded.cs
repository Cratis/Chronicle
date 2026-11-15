// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Bounds every fence read and write by the fence timeout.
/// </summary>
/// <remarks>
/// The server-side time limit and the write concern timeout only start once a server is selected; an unreachable server holds the call for the
/// driver's server selection timeout, 30 seconds by default, which is the whole Orleans call budget. The cancellation
/// token covers server selection too, and waiting on it covers any driver step that ignores the token.
/// </remarks>
public partial class EncryptionKeyStorage
{
    /// <summary>
    /// Run a fence operation within the fence timeout.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="identifier">The identifier the fence belongs to.</param>
    /// <param name="operation">The operation, given the token that ends with the timeout.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The server could not answer within the timeout, or failed.</exception>
    /// <remarks>A duplicate-key write is a definite non-write the callers act on, so it passes through unchanged.</remarks>
    internal Task<T> Bounded<T>(EncryptionKeyIdentifier identifier, Func<CancellationToken, Task<T>> operation) =>
        Bounded(operation, _ => new EncryptionKeyFenceUnavailable(identifier));

    /// <summary>
    /// Run a fence operation without a result within the fence timeout.
    /// </summary>
    /// <param name="identifier">The identifier the fence belongs to.</param>
    /// <param name="operation">The operation, given the token that ends with the timeout.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The server could not answer within the timeout, or failed.</exception>
    internal Task Bounded(EncryptionKeyIdentifier identifier, Func<CancellationToken, Task> operation) =>
        Bounded(Unit(operation), _ => new EncryptionKeyFenceUnavailable(identifier));

    /// <summary>
    /// Run a fence metadata operation, which is not addressed by an identifier, within the fence timeout.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation, given the token that ends with the timeout.</param>
    /// <returns>The operation's result.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The server could not answer within the timeout, or failed.</exception>
    internal Task<T> BoundedRecord<T>(Func<CancellationToken, Task<T>> operation) =>
        Bounded(operation, cause => new EncryptionKeyFenceUnavailable(cause));

    /// <summary>
    /// Run a fence metadata operation without a result within the fence timeout.
    /// </summary>
    /// <param name="operation">The operation, given the token that ends with the timeout.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The server could not answer within the timeout, or failed.</exception>
    internal Task BoundedRecord(Func<CancellationToken, Task> operation) =>
        Bounded(Unit(operation), cause => new EncryptionKeyFenceUnavailable(cause));

    /// <summary>
    /// Run a fence write within the fence timeout, reporting a write that may have been applied as an unknown outcome.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="identifier">The identifier the fence belongs to.</param>
    /// <param name="operation">The write, given the token that ends with the timeout.</param>
    /// <returns>The write's result.</returns>
    /// <exception cref="EncryptionKeyFenceWriteOutcomeUnknown">The write timed out, was cancelled or lost its response.</exception>
    /// <exception cref="EncryptionKeyFenceUnavailable">The write failed before it was sent, or the server rejected it.</exception>
    internal Task<T> BoundedWrite<T>(EncryptionKeyIdentifier identifier, Func<CancellationToken, Task<T>> operation) =>
        Bounded(operation, cause => IsUnknownOutcome(cause) ? new EncryptionKeyFenceWriteOutcomeUnknown(identifier, cause) : new EncryptionKeyFenceUnavailable(identifier));

    /// <summary>
    /// Run a fence write without a result within the fence timeout, reporting a write that may have been applied as an unknown outcome.
    /// </summary>
    /// <param name="identifier">The identifier the fence belongs to.</param>
    /// <param name="operation">The write, given the token that ends with the timeout.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EncryptionKeyFenceWriteOutcomeUnknown">The write timed out, was cancelled or lost its response.</exception>
    /// <exception cref="EncryptionKeyFenceUnavailable">The write failed before it was sent, or the server rejected it.</exception>
    internal Task BoundedWrite(EncryptionKeyIdentifier identifier, Func<CancellationToken, Task> operation) =>
        BoundedWrite(identifier, Unit(operation));

    /// <summary>
    /// Run a fence metadata write, which is not addressed by an identifier, within the fence timeout.
    /// </summary>
    /// <param name="operation">The write, given the token that ends with the timeout.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="EncryptionKeyFenceWriteOutcomeUnknown">The write timed out, was cancelled or lost its response.</exception>
    /// <exception cref="EncryptionKeyFenceUnavailable">The write failed before it was sent, or the server rejected it.</exception>
    internal Task BoundedRecordWrite(Func<CancellationToken, Task> operation) =>
        Bounded(Unit(operation), cause => IsUnknownOutcome(cause) ? new EncryptionKeyFenceWriteOutcomeUnknown(cause) : new EncryptionKeyFenceUnavailable(cause));

    /// <summary>
    /// Decide whether a failed write may still have been applied.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns><see langword="true"/> unless the failure proves the write was not applied.</returns>
    /// <remarks>
    /// A rejected write or a server-selection timeout applied nothing. The driver does not say whether a cancelled call reached the
    /// server, so cancellation, like a dropped connection, a server-side time limit or a missed write concern, is unknown.
    /// </remarks>
    static bool IsUnknownOutcome(Exception exception) => exception switch
    {
        TimeoutException timeout => !timeout.Message.Contains("selecting a server", StringComparison.Ordinal),
        MongoWriteException write => write.WriteError is null && write.WriteConcernError is not null,
        OperationCanceledException or MongoConnectionException or MongoExecutionTimeoutException or MongoWriteConcernException => true,
        _ => false
    };

    static Func<CancellationToken, Task<bool>> Unit(Func<CancellationToken, Task> operation) =>
        async cancellationToken =>
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return true;
        };

    async Task<T> Bounded<T>(Func<CancellationToken, Task<T>> operation, Func<Exception, Exception> unavailable)
    {
        using var timeout = new CancellationTokenSource(_readTimeout);
        try
        {
            return await operation(timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (MongoWriteException exception) when (IsDuplicate(exception))
        {
            throw;
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException or OperationCanceledException)
        {
            throw unavailable(exception);
        }
    }

    /// <summary>
    /// Get the key collection for a fence operation, bounding the one-time index creation as well.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The identifier the fence belongs to.</param>
    /// <returns>The key collection.</returns>
    Task<IMongoCollection<EncryptionKeyForIdentifier>> GetFencedCollection(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        Bounded(identifier, _ => GetCollection(eventStore, eventStoreNamespace));

    async Task VerifySwept(IMongoCollection<EncryptionKeyForIdentifier> collection, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        if (await Bounded(identifier, cancellationToken => collection.WithReadConcern(ReadConcern.Linearizable).WithReadPreference(ReadPreference.Primary)
            .Find(Covered(identifier, request), new FindOptions { MaxTime = _readTimeout }).Limit(1).AnyAsync(cancellationToken)))
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }
}
