// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Attribute used to mark a reactor class as one that should not observe history that predates its first registration.
/// The reactor begins at the tail of the event sequence the first time Chronicle sees it, and observes only what is
/// appended from then on.
/// </summary>
/// <remarks>
/// <para>
/// A new reactor normally catches up from the very beginning of the event sequence, as ordinary observation, so a
/// reactor whose handlers cause side effects - sending a notification, calling an external system, starting a job -
/// would act on every historical event the moment it is deployed. <see cref="OnceOnlyAttribute"/> does not prevent
/// that, because it only excludes replay. This attribute does.
/// </para>
/// <para>
/// It only affects the first registration. A reactor that Chronicle already holds a position for keeps it, so the
/// attribute is safe to leave in place and does not skip events after a restart. Renaming the reactor or moving its
/// namespace changes its default identifier, which makes it a new reactor, and it begins at the tail again - unless
/// an explicit identifier is given on <see cref="ReactorAttribute"/>. An explicit replay still replays from the
/// beginning, because the attribute only chooses where a reactor starts.
/// </para>
/// <para>
/// Both the Chronicle server and the client must be on a version that supports it. A server that does not know about
/// it ignores the request and the reactor catches up from the beginning.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StartAtTailAttribute : Attribute;
