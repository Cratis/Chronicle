// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Concepts.Projections.Json;

public interface IJsonProjectionChangesetSerializer
{
    /// <summary>
    /// Serialize a <see cref="IChangeset{TSource, TTarget}"/>.
    /// </summary>
    /// <typeparam name="TSource">The source.</typeparam>
    /// <typeparam name="TTarget">The target.</typeparam>
    /// <param name="changeset"><see cref="IChangeset{TSource, TTarget}"/> to serialize.</param>
    /// <returns>JSON representation.</returns>
    JsonNode Serialize<TSource, TTarget>(IChangeset<TSource, TTarget> changeset);
}