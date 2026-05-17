// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Attribute that marks a reactor or reducer as non-replayable.
/// Non-replayable observers process events normally but are excluded from replay operations
/// triggered by redaction, revision, or observer rewinding.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class NonReplayableAttribute : Attribute;
