// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Defines a patch that fans out across event stores and leaves quarantined ones out.
/// </summary>
/// <remarks>
/// <para>
/// A patch that left an event store out has not reached it. The <see cref="PatchManager"/> therefore does not record
/// the patch as applied while <see cref="SkippedEventStores"/> is not empty, and does not move the system version
/// past it, so the patch runs again on a later startup, once the quarantine is resolved.
/// </para>
/// <para>
/// The patches that do this are idempotent per event store - they remove, or rebuild, derived data - so running them
/// again over the stores they already reached repeats work but changes nothing. That is why a single applied record
/// is enough and no per-store record is kept.
/// </para>
/// </remarks>
public interface IReportSkippedEventStores
{
    /// <summary>
    /// Gets the event stores the last run of the patch left out.
    /// </summary>
    IReadOnlyList<EventStoreName> SkippedEventStores { get; }
}
