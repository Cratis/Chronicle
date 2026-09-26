// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Attribute used to mark a reactor handler method as one that should not run while the observer is working
/// through a backlog.
/// </summary>
/// <remarks>
/// For a handler whose effect is to act on the world as it is now - notify someone, call an external system -
/// an event from three days ago is not an instruction to act, it is history. The observer still advances past
/// the event, exactly as it does for a handler skipped by <see cref="OnceOnlyAttribute"/> during a replay;
/// skipping without advancing would leave the backlog undrained and be worse than the problem.
/// <para>
/// This is the sibling of <see cref="OnceOnlyAttribute"/>, which covers replay. A handler that should only
/// ever fire for something that genuinely just happened wants both.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SkipCatchUpAttribute : Attribute;
