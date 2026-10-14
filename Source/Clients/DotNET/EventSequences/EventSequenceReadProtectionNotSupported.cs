// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when a sequence cannot accept a trusted read-protection commit.
/// </summary>
/// <param name="type">The unsupported sequence implementation.</param>
public class EventSequenceReadProtectionNotSupported(Type type) : Exception($"Event sequence '{type.FullName}' does not support trusted read-protection commits.");
