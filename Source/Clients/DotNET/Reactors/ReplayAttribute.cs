// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Attribute used to mark a reactor handler method as the one to run while events are being replayed.
/// </summary>
/// <remarks>
/// A reactor sees the same events twice for different reasons: as they happen, and again when its observer is
/// replayed. Those often call for different work - a notification that should go out once when the event happens
/// has no business going out again during a rebuild. Mark a second handler for the same event type with this
/// attribute and it takes over for the duration of the replay, leaving the unmarked handler to the live path.
/// <para>
/// When an event type has no handler marked with this attribute, the regular handler runs during replay as
/// before. When it has one, only the marked handler runs during replay - the regular handler does not also run.
/// </para>
/// <para>
/// Use <see cref="OnceOnlyAttribute"/> instead when the side effect should simply not happen again on replay.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ReplayAttribute : Attribute;
