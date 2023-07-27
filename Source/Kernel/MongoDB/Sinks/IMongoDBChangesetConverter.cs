// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a system that can convert a <see cref="IChangeset{TEvent, TState}"/> to a <see cref="UpdateDefinition{TDocument}"/> for MongoDB.
/// </summary>
public interface IMongoDBChangesetConverter
{
    /// <summary>
    /// Convert a <see cref="IChangeset{TEvent, TState}"/> to a <see cref="UpdateDefinition{TDocument}"/> for MongoDB.
    /// </summary>
    /// <param name="key"><see cref="Key"/> to use.</param>
    /// <param name="changeset"><see cref="Changeset{T1, T2}"/> to convert from.</param>
    /// <param name="isReplaying">Whether or not it is for a replay or not.</param>
    /// <returns>A <see cref="MongoDBUpdateDefinitionAndArrayFilters"/> instance.</returns>
    Task<MongoDBUpdateDefinitionAndArrayFilters> ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying);
}
