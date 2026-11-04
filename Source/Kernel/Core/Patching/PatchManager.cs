// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Sys;
using Orleans.Placement;
using ChoreographyPatchManager = Cratis.Choreography.Patching.IPatchManager;
using ChoreographyPatchOutcome = Cratis.Choreography.Patching.PatchOutcome;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchManager"/> that hands the work to the patch manager of Cratis Choreography.
/// </summary>
/// <remarks>
/// A release that patches through Cratis Choreography no longer asks this grain. A silo of an earlier release still does while a rolling
/// upgrade is in progress, and expects this grain interface to be served by the new silos, so it stays and answers on behalf of
/// the manager that does the work now.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting the patch manager of Cratis Choreography.</param>
[PreferLocalPlacement]
public class PatchManager(IGrainFactory grainFactory) : Grain, IPatchManager
{
    /// <summary>
    /// The key the patch manager of Cratis Choreography reads the silo of the caller from, when it has to move to a newer release.
    /// </summary>
    internal const string ChoreographyCallerSiloKey = "Cratis.Choreography.Patching.CallerSilo";

    /// <inheritdoc/>
    public async Task ApplyPatches() => await ApplyPatchesFor(KernelVersion.Current);

    /// <inheritdoc/>
    public async Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller)
    {
        // The request context flows to the grain that does the work. An earlier release put the silo that should receive
        // the manager under its own key; hand it over under the key the manager reads.
        if (PatchManagerRelocation.CallerSilo() is { } callerSilo)
        {
            RequestContext.Set(ChoreographyCallerSiloKey, callerSilo);
        }

        var outcome = await grainFactory.GetGrain<ChoreographyPatchManager>(0).ApplyPatchesFor(caller.ToPatching());
        return outcome == ChoreographyPatchOutcome.Relocate ? PatchOutcome.Relocate : PatchOutcome.Completed;
    }
}
