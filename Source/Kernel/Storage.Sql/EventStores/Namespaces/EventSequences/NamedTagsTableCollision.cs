// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>The exception thrown when an existing table occupies Chronicle's reserved named-tag table name.</summary>
public class NamedTagsTableCollision() : Exception("The reserved SQL table __cratis_named_tags already exists but is not a Chronicle named-tags table, or an event sequence uses that name. No data was changed.");
