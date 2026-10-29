// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_EventSequence.given;

/// <summary>
/// A document joined to a value supplied by another event source.
/// </summary>
/// <param name="Id">The document key.</param>
/// <param name="ReferenceId">The joined reference.</param>
/// <param name="Amount">The joined value.</param>
public record JoinedReadModel(string Id, string ReferenceId, int Amount);
