// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.EventSequences.Queries;

/// <summary>
/// Represents information about an event sequence.
/// </summary>
/// <param name="Id">Identifier of the event sequence.</param>
/// <param name="Name">Name of the sequence.</param>
public record EventSequenceInformation(string Id, string Name);
