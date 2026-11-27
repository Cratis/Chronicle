// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Namespaces;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces;

/// <summary>Encodes the complete control record without relying on application BSON conventions.</summary>
internal static class NamespaceLifecycleDocument
{
    /// <summary>Encodes control state under a case-insensitive namespace identity.</summary>
    /// <param name="state">The complete state.</param>
    /// <returns>The replacement document.</returns>
    internal static BsonDocument Encode(NamespaceLifecycle state) => new()
    {
        { "_id", state.Namespace.Value.ToUpperInvariant() },
        { "name", state.Namespace.Value },
        { "incarnation", Uuid(state.Incarnation.Value) },
        { "created", Timestamp(state.Created) },
        { "revision", state.Revision },
        { "previousIncarnation", state.PreviousIncarnation is { } previous ? Uuid(previous.Value) : BsonNull.Value },
        { "notifyOnOpen", state.NotifyOnOpen },
        { "status", (int)state.Status },
        { "operationId", Uuid(state.OperationId) },
        { "stage", (int)state.Stage },
        { "stageAttempts", state.StageAttempts },
        { "totalAttempts", state.TotalAttempts },
        { "attemptInProgress", state.AttemptInProgress },
        { "destructiveAttemptOutstanding", state.DestructiveAttemptOutstanding },
        { "destructiveActionIssued", state.DestructiveActionIssued },
        { "lastAttemptAt", Timestamp(state.LastAttemptAt) },
        { "retryAt", Timestamp(state.RetryAt) },
        { "deadline", Timestamp(state.Deadline) },
        { "lastFailure", state.LastFailure }
    };

    /// <summary>Decodes the original display casing and complete recovery evidence.</summary>
    /// <param name="document">The stored document.</param>
    /// <returns>The control state.</returns>
    internal static NamespaceLifecycle Decode(BsonDocument document) => new(
        document["name"].AsString,
        new(ReadUuid(document["incarnation"])),
        ReadTimestamp(document["created"])!.Value)
    {
        Revision = document["revision"].AsInt64,
        PreviousIncarnation = document["previousIncarnation"].IsBsonNull ? null : new(ReadUuid(document["previousIncarnation"])),
        NotifyOnOpen = document["notifyOnOpen"].AsBoolean,
        Status = (NamespaceLifecycleStatus)document["status"].AsInt32,
        OperationId = ReadUuid(document["operationId"]),
        Stage = (NamespaceRemovalStage)document["stage"].AsInt32,
        StageAttempts = document["stageAttempts"].AsInt32,
        TotalAttempts = document["totalAttempts"].AsInt32,
        AttemptInProgress = document["attemptInProgress"].AsBoolean,
        DestructiveAttemptOutstanding = document["destructiveAttemptOutstanding"].AsBoolean,
        DestructiveActionIssued = document["destructiveActionIssued"].AsBoolean,
        LastAttemptAt = ReadTimestamp(document["lastAttemptAt"]),
        RetryAt = ReadTimestamp(document["retryAt"]),
        Deadline = ReadTimestamp(document["deadline"]),
        LastFailure = document["lastFailure"].AsString
    };

    static BsonBinaryData Uuid(Guid value) => new(value, GuidRepresentation.Standard);

    static Guid ReadUuid(BsonValue value) => value.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);

    static BsonValue Timestamp(DateTimeOffset? value) => value is { } time ? new BsonString(time.ToString("O", CultureInfo.InvariantCulture)) : BsonNull.Value;

    static DateTimeOffset? ReadTimestamp(BsonValue value) => value.IsBsonNull ? null : DateTimeOffset.Parse(value.AsString, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
