// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Defines a system that can convert a <see cref="IChangeset{TEvent, TState}"/> to a <see cref="UpdateDefinition{TDocument}"/> for MongoDB.
/// </summary>
public interface IChangesetConverter
{
    /// <summary>
    /// Convert a <see cref="IChangeset{TEvent, TState}"/> to a <see cref="UpdateDefinition{TDocument}"/> for MongoDB.
    /// </summary>
    /// <param name="key"><see cref="Key"/> to use.</param>
    /// <param name="changeset"><see cref="Changeset{T1, T2}"/> to convert from.</param>
    /// <param name="isReplaying">Whether or not it is for a replay or not.</param>
    /// <returns>A <see cref="UpdateDefinitionAndArrayFilters"/> instance.</returns>
    Task<UpdateDefinitionAndArrayFilters> ToUpdateDefinition(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying);
}
