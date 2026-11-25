// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Identifies a rebuild run without depending on the runtime's job types.
/// </summary>
/// <param name="Value">The nonempty run identifier.</param>
public sealed record UniqueConstraintRunId(Guid Value) : ConceptAs<Guid>(Value != Guid.Empty ? Value : throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintRunId)));
