// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when the bound event sequence cannot apply implicit guards.
/// </summary>
/// <param name="implementation">The event sequence implementation.</param>
public class EventSequenceImplicitScopesNotSupported(Type implementation)
    : Exception($"Event sequence '{implementation.FullName}' does not support implicit scopes.");
