// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.given;

/// <summary>
/// A namespace of a kernel whose pattern capture is switched off for every event store.
/// </summary>
/// <param name="fixture">The fixture that hosts the in-process kernel.</param>
public class a_namespace_without_pattern_capture(PatternCaptureOffFixture fixture) : a_dormant_reactor(fixture)
{
    protected override void ConfigureObserverSettings(Dictionary<string, string?> settings) =>
        settings["PatternDetection:Enabled"] = "false";

    protected async Task<string[]> Resident(string grainType, string containing) =>
        [.. (await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics())
            .Select(statistics => statistics.GrainId.ToString())
            .Where(grain =>
                grain.StartsWith(grainType, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(containing, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(_store.Name.Value, StringComparison.Ordinal))];
}
