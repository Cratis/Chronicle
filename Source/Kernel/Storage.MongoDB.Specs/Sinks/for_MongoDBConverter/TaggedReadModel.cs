// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;

/// <summary>
/// A read model carrying a top-level list of bare string concepts.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Tags">The list of bare string concepts.</param>
public record TaggedReadModel(Guid Id, IEnumerable<TagConcept> Tags);
