// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.Kernel.Integration.for_PatternCapture.given;

/// <summary>
/// A namespace of a kernel that releases a subscribed observer after two quiet seconds and persists mined patterns every two.
/// </summary>
/// <param name="fixture">The fixture that hosts the in-process kernel.</param>
public class a_pattern_capturing_namespace(PatternCaptureFixture fixture) : a_dormant_reactor<PatternCaptureFixture>(fixture)
{
    protected override void ConfigureObserverSettings(Dictionary<string, string?> settings) =>
        settings["PatternDetection:PersistenceInterval"] = "2";

    /// <summary>
    /// Gets the activations in the kernel whose identity names a grain type and holds the event store of the namespace.
    /// </summary>
    /// <param name="grainType">The lower case prefix of the grain type, such as <c language="csharp">observer/</c>.</param>
    /// <param name="containing">Text the identity must also hold.</param>
    /// <returns>The identities holding an activation.</returns>
    protected async Task<string[]> Resident(string grainType, string containing) =>
        [.. (await Services.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0).GetDetailedGrainStatistics())
            .Select(statistics => statistics.GrainId.ToString())
            .Where(grain =>
                grain.StartsWith(grainType, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(containing, StringComparison.OrdinalIgnoreCase) &&
                grain.Contains(_store.Name.Value, StringComparison.Ordinal))];

    protected Task<string[]> ResidentCaptureObservers() => Resident(ObserverGrainPrefix, PatternCapture.ObserverIdentifier);

    protected Task<string[]> ResidentMiners() => Resident("patternminer", string.Empty);

    protected async Task<bool> CaptureIsRegistered() =>
        await Services.GetRequiredService<Storage.IStorage>().GetEventStore(_store.Name.Value).Reactors.Has(PatternCapture.ObserverIdentifier);

    protected async Task<bool> WaitUntil(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (!await condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(500);
        }

        return true;
    }
}
