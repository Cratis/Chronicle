// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Fence = Cratis.Chronicle.Storage.Compliance.EncryptionKeyErasure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Provider-portable, single-statement writes, without a retrying execution strategy.
/// </summary>
internal static class EncryptionKeyFenceWrites
{
    /// <summary>
    /// Seal canonical fence semantics, including the immutable request log.
    /// </summary>
    /// <param name="fence">The fence.</param>
    /// <returns>The sealed fence.</returns>
    internal static Fence WithStateSeal(this Fence fence) => fence with { Requests = fence.Requests ?? [], Seal = Seal(fence) };

    /// <summary>
    /// Validate the seal before interpreting any fence fields.
    /// </summary>
    /// <param name="fence">The fence.</param>
    /// <returns>Whether the seal matches.</returns>
    internal static bool HasValidStateSeal(this Fence fence) => StringComparer.Ordinal.Equals(fence.Seal, Seal(fence));

    /// <summary>
    /// Quote a fixed schema identifier for the active provider.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="name">The schema identifier.</param>
    /// <returns>The quoted identifier.</returns>
    internal static string Name(NamespaceDbContext context, string name) => context.GetService<ISqlGenerationHelper>().DelimitIdentifier(name);

    /// <summary>
    /// Execute once. SQL contains only quoted schema identifiers; all values are parameters.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="sql">The statement with parameter placeholders.</param>
    /// <param name="parameters">The values.</param>
    /// <returns>The affected row count.</returns>
    internal static Task<int> Execute(NamespaceDbContext context, string sql, params object[] parameters) => context.Database.ExecuteSqlRawAsync(sql, parameters.Select(_ => _ is uint value ? (long)value : _));

    /// <summary>
    /// Read a sealed fence or conservatively interpret an entirely legacy row.
    /// </summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="row">The persisted row.</param>
    /// <returns>The validated fence.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The stored metadata cannot be trusted.</exception>
    internal static Fence Read(EncryptionKeyIdentifier identifier, EncryptionKeyErasure row)
    {
        var legacy = row.Version is null && row.Lifecycle is null && row.ErasedLifecycle is null && row.StateSeal is null && row.Requests is null;
        try
        {
            if (row.Version is { } version && decimal.Truncate(version) != version) throw new EncryptionKeyFenceUnavailable(identifier);
            var fence = new Fence(
                new(row.ErasedThrough),
                row.ErasedKeyFingerprints.Split(',', StringSplitOptions.RemoveEmptyEntries),
                row.NewKeyAllowed,
                checked((ulong)(row.Version ?? 0)),
                row.Lifecycle ?? 1,
                row.ErasedLifecycle ?? 0,
                row.Requests is null ? [] : JsonSerializer.Deserialize<EncryptionKeyErasureRequest[]>(row.Requests),
                row.StateSeal);
            if (legacy) return fence.WithStateSeal();
            if (row.Version is null || row.Lifecycle is null || row.ErasedLifecycle is null || row.Requests is null || fence.Requests is null || !fence.HasValidStateSeal())
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            return fence;
        }
        catch (Exception exception) when (exception is JsonException or OverflowException or ArgumentException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    /// <summary>
    /// Initialize only an absent fence, tolerating only definite unique conflicts.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="identifier">The subject.</param>
    /// <param name="fence">The initial fence.</param>
    /// <returns>Whether insertion won.</returns>
    internal static async Task<bool> InsertFence(NamespaceDbContext context, EncryptionKeyIdentifier identifier, Fence fence)
    {
        string N(string name) => Name(context, name);
        try
        {
            return await Write(identifier, () => Execute(
                context,
                $"INSERT INTO {N("EncryptionKeyErasures")} ({N("Identifier")}, {N("ErasedThrough")}, {N("ErasedKeyFingerprints")}, {N("NewKeyAllowed")}, {N("Version")}, {N("Lifecycle")}, {N("StateSeal")}, {N("Requests")}, {N("ErasedLifecycle")}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}})",
                [EncryptionKeyIdentifierColumn.Value(context, identifier), fence.ErasedThrough.Value, string.Join(',', fence.ErasedKeyFingerprints), fence.NewKeyAllowed, (decimal)fence.Version, fence.Lifecycle, fence.Seal!, JsonSerializer.Serialize(fence.Requests), fence.ErasedLifecycle])) == 1;
        }
        catch (DbException exception) when (IsDuplicate(exception))
        {
            return false;
        }
    }

    /// <summary>
    /// Replace only the observed version and canonical legacy fields.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="identifier">The subject.</param>
    /// <param name="observed">The observed row, including nullable legacy metadata.</param>
    /// <param name="next">The sealed monotonic result.</param>
    /// <returns>The affected row count.</returns>
    internal static Task<int> CompareExchange(NamespaceDbContext context, EncryptionKeyIdentifier identifier, EncryptionKeyErasure observed, Fence next)
    {
        string N(string name) => Name(context, name);
        var version = observed.Version is null ? $"{N("Version")} IS NULL" : $"{N("Version")} = {{8}}";
        return Write(identifier, () => Execute(
            context,
            $"UPDATE {N("EncryptionKeyErasures")} SET {N("ErasedThrough")} = {{0}}, {N("ErasedKeyFingerprints")} = {{1}}, {N("NewKeyAllowed")} = {{2}}, {N("Version")} = {{3}}, {N("Lifecycle")} = {{4}}, {N("StateSeal")} = {{5}}, {N("Requests")} = {{6}}, {N("ErasedLifecycle")} = {{12}} WHERE {N("Identifier")} = {{7}} AND {version} AND {N("ErasedThrough")} = {{9}} AND {N("ErasedKeyFingerprints")} = {{10}} AND {N("NewKeyAllowed")} = {{11}}",
            [next.ErasedThrough.Value, string.Join(',', next.ErasedKeyFingerprints), next.NewKeyAllowed, (decimal)next.Version, next.Lifecycle, next.Seal!, JsonSerializer.Serialize(next.Requests), EncryptionKeyIdentifierColumn.Value(context, identifier), observed.Version ?? 0, observed.ErasedThrough, observed.ErasedKeyFingerprints, observed.NewKeyAllowed, next.ErasedLifecycle]));
    }

    /// <summary>
    /// Insert an unpublished, uniquely owned key row.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="identifier">The subject.</param>
    /// <param name="revision">The revision.</param>
    /// <param name="key">The key material.</param>
    /// <param name="lifecycle">The pinned lifecycle.</param>
    /// <param name="nonce">The write owner.</param>
    /// <param name="replacePublished">Whether an explicit save may replace an existing published revision.</param>
    /// <returns>The affected row count.</returns>
    internal static async Task<int> Stage(NamespaceDbContext context, EncryptionKeyIdentifier identifier, uint revision, Compliance.EncryptionKey key, uint lifecycle, string nonce, bool replacePublished)
    {
        string N(string name) => Name(context, name);
        if (replacePublished)
        {
            var replaced = await Write(identifier, () => Execute(
                context,
                $"UPDATE {N("EncryptionKeys")} SET {N("PublicKey")} = {{0}}, {N("PrivateKey")} = {{1}}, {N("StagedLifecycle")} = {{2}}, {N("WriteNonce")} = {{3}}, {N("Published")} = {{4}} WHERE {N("Identifier")} = {{5}} AND {N("Revision")} = {{6}} AND ({N("Published")} IS NULL OR {N("Published")} = {{7}})",
                [key.Public, key.Private, lifecycle, nonce, false, EncryptionKeyIdentifierColumn.Value(context, identifier), revision, true]));
            if (replaced == 1) return replaced;
        }

        return await Write(identifier, () => Execute(
            context,
            $"INSERT INTO {N("EncryptionKeys")} ({N("Identifier")}, {N("Revision")}, {N("PublicKey")}, {N("PrivateKey")}, {N("StagedLifecycle")}, {N("WriteNonce")}, {N("Published")}) VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}})",
            [EncryptionKeyIdentifierColumn.Value(context, identifier), revision, key.Public, key.Private, lifecycle, nonce, false]));
    }

    /// <summary>
    /// Publish only the writer's row while the validated fence remains current and open.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="identifier">The subject.</param>
    /// <param name="revision">The revision.</param>
    /// <param name="nonce">The write owner.</param>
    /// <param name="fence">The validated fence.</param>
    /// <returns>The affected row count.</returns>
    internal static Task<int> Publish(NamespaceDbContext context, EncryptionKeyIdentifier identifier, uint revision, string nonce, Fence fence)
    {
        string N(string name) => Name(context, name);
        return Write(identifier, () => Execute(
            context,
            $"UPDATE {N("EncryptionKeys")} SET {N("Published")} = {{0}} WHERE {N("Identifier")} = {{1}} AND {N("Revision")} = {{2}} AND {N("WriteNonce")} = {{3}} AND {N("StagedLifecycle")} = {{4}} AND EXISTS (SELECT 1 FROM {N("EncryptionKeyErasures")} WHERE {N("Identifier")} = {{1}} AND {N("Version")} = {{5}} AND {N("StateSeal")} = {{6}} AND {N("NewKeyAllowed")} = {{0}})",
            [true, EncryptionKeyIdentifierColumn.Value(context, identifier), revision, nonce, fence.Lifecycle, (decimal)fence.Version, fence.Seal!]));
    }

    /// <summary>
    /// Run a fence write, reporting a write that may have been applied as an unknown outcome.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="identifier">The subject, or <see langword="null"/> for fence metadata.</param>
    /// <param name="write">The write.</param>
    /// <returns>The write's result.</returns>
    /// <exception cref="EncryptionKeyFenceWriteOutcomeUnknown">The write timed out, was cancelled or lost its connection.</exception>
    /// <remarks>Failures that prove nothing was applied, such as a duplicate key, pass through unchanged.</remarks>
    internal static async Task<T> Write<T>(EncryptionKeyIdentifier? identifier, Func<Task<T>> write)
    {
        try
        {
            return await write();
        }
        catch (Exception exception) when (IsUnknownOutcome(exception))
        {
            throw identifier is null ? new EncryptionKeyFenceWriteOutcomeUnknown(exception) : new EncryptionKeyFenceWriteOutcomeUnknown(identifier, exception);
        }
    }

    /// <summary>
    /// Decide whether a failed write may still have been applied.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <returns><see langword="true"/> unless the server reported an error, which rolls the single statement back.</returns>
    /// <remarks>
    /// Client-side failures cannot tell whether the statement reached the server: a timeout, a cancellation, a client-side
    /// Npgsql failure or a SQL Server timeout or connection-terminating error. SQLite runs in-process and reports every outcome.
    /// </remarks>
    internal static bool IsUnknownOutcome(Exception exception) => exception switch
    {
        TimeoutException or OperationCanceledException => true,
        Npgsql.PostgresException => false,
        Npgsql.NpgsqlException => true,
        Microsoft.Data.SqlClient.SqlException sql => sql.Number == -2 || sql.Class >= 20,
        _ => false
    };

    /// <summary>
    /// Recognize only definite duplicate-key failures, never transient or unknown outcomes.
    /// </summary>
    /// <param name="exception">The provider exception.</param>
    /// <returns>Whether no insertion took place.</returns>
    internal static bool IsDuplicate(DbException exception) => exception switch
    {
        Npgsql.PostgresException postgres => postgres.SqlState == "23505",
        Microsoft.Data.SqlClient.SqlException sql => sql.Number is 2601 or 2627,
        Microsoft.Data.Sqlite.SqliteException sqlite => sqlite.SqliteExtendedErrorCode is 1555 or 2067,
        _ => false
    };

    static string Seal(Fence fence) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        fence.Version,
        Through = fence.ErasedThrough.Value,
        Fingerprints = fence.ErasedKeyFingerprints.Order(StringComparer.Ordinal).ToArray(),
        fence.NewKeyAllowed,
        fence.Lifecycle,
        fence.ErasedLifecycle,
        Requests = (fence.Requests ?? []).OrderBy(_ => _.RequestId, StringComparer.Ordinal).ToArray()
    })));
}
