// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a unique commit token used to resolve ambiguous commit outcomes.
/// </summary>
/// <param name="Value">A new GUID for each commit.</param>
public sealed record DeliveryCommitId(Guid Value) : ConceptAs<Guid>(Value);
