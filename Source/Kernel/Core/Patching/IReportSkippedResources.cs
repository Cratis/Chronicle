// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStores;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Defines a patch that fans out across event stores and namespaces and leaves quarantined ones out.
/// </summary>
/// <remarks>
/// <para>
/// A patch that left an event store or namespace out has not reached it. The <see cref="ChroniclePatchStorage"/> therefore does
/// not record the patch as applied while <see cref="SkippedResources"/> has anything in it, and does not move the
/// system version past it, so the patch runs again on a later startup, once the quarantine is resolved.
/// </para>
/// <para>
/// The patches that do this are idempotent per event store - they remove, or rebuild, derived data - so running them
/// again over the stores they already reached repeats work but changes nothing. That is why a single applied record
/// is enough and no per-store record is kept.
/// </para>
/// <para>
/// The skipped resources live on the patch instance, so <see cref="PatchingSiloBuilderExtensions.AddChroniclePatching(ISiloBuilder)"/>
/// registers every patch that implements this as a single shared instance: the one that ran is the one that is asked.
/// </para>
/// </remarks>
public interface IReportSkippedResources
{
    /// <summary>
    /// Gets the event stores and namespaces the last run of the patch left out.
    /// </summary>
    SkippedResources SkippedResources { get; }
}
