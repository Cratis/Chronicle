// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// The exception that is thrown when a database name Chronicle composed is not a legal MongoDB database name.
/// </summary>
/// <remarks>
/// Chronicle composes its database names from the event store name and the namespace. When Arc resolves the
/// namespace, the namespace is the tenant id — so an otherwise innocent looking tenant id such as one containing
/// a space produces a name MongoDB rejects. Without this check MongoDB fails much later with a bare
/// <c>Invalid namespace specified</c> that names neither the event store nor the namespace that caused it.
/// </remarks>
/// <param name="databaseName">The composed database name.</param>
/// <param name="reason">Why the name is not legal.</param>
/// <param name="eventStore">The event store name it was composed from.</param>
/// <param name="namespace">The namespace it was composed from, if any.</param>
public class InvalidDatabaseName(string databaseName, string reason, string eventStore, string? @namespace)
    : Exception($"Chronicle composed the MongoDB database name '{databaseName}' from event store '{eventStore}'{(@namespace is null ? string.Empty : $" and namespace '{@namespace}'")}, but {reason}. Both the event store name and the namespace — which is the tenant id when Arc resolves it — must be legal MongoDB database name tokens.");
