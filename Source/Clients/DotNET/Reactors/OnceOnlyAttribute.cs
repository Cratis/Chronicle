// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Attribute used to mark a reactor class or individual handler methods as non-replayable.
/// When applied to a class, the entire reactor is excluded from all replay operations
/// (redaction, revision, observer rewind). When applied to a method, only that handler
/// is excluded from replay.
/// </summary>
/// <remarks>
/// The two placements are enforced at different points. A class-level placement registers the reactor as
/// non-replayable, so a replay of the observer never starts. A method-level placement leaves the reactor
/// replayable - its other handlers still need to replay - and the marked handler is skipped for every event
/// that arrives as part of a replay.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class OnceOnlyAttribute : Attribute;
