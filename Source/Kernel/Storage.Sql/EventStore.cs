// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an event store.
/// </summary>
/// <param name="Name">Name of event store.</param>
public record EventStore(EventStoreName Name);
