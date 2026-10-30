// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

/// <summary>
/// Represents the publication receipt id.
/// </summary>
/// <param name="Value">The value.</param>
public record PublicationReceiptId(Guid Value) : ConceptAs<Guid>(Value);
