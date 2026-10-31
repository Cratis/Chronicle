// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The exception that is thrown when a definition-change page has a nonpositive requested limit.
/// </summary>
/// <param name="limit">The invalid limit.</param>
public class InvalidObserverDefinitionChangePageLimit(int limit) : Exception($"Definition-change page limit must be positive, got {limit}.");
