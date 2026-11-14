// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// An event store and namespace an operator listed as holding keys under earlier names.
/// </summary>
/// <param name="EventStore">The event store name.</param>
/// <param name="EventStoreNamespace">The namespace name.</param>
public record LegacyScope(string EventStore, string EventStoreNamespace);
