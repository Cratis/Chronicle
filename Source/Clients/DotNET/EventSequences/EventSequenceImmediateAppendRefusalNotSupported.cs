// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when a sequence has no trusted commit path for an immediate-append refusal frame.
/// </summary>
/// <param name="type">The unsupported implementation.</param>
public class EventSequenceImmediateAppendRefusalNotSupported(Type type) : Exception($"Event sequence implementation '{type.FullName}' does not support committing through an immediate-append refusal frame.");
