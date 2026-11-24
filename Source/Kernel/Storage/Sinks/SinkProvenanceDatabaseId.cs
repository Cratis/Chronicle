// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Identifies one physical read-model database, independently of its logical routes.
/// </summary>
/// <param name="Value">The database-local identity.</param>
public record SinkProvenanceDatabaseId(Guid Value) : ConceptAs<Guid>(Value);
