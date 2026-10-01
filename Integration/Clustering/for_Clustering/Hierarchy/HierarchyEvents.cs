// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering.Hierarchy;

public static class HierarchyEvents
{
    public const int Modules = 12;
    public const int FeaturesPerModule = 2;
    public const int SlicesPerFeature = 3;
    public const int EventsPerSlice = 2;

    public const int Features = Modules * FeaturesPerModule;
    public const int Slices = Features * SlicesPerFeature;
    public const int SliceEvents = Slices * EventsPerSlice;

    public static async Task<(Guid[] Modules, EventSequenceNumber Last)> Append(IEventStore eventStore)
    {
        // Interleaves the modules so that consecutive events belong to different documents, the way several
        // people editing several boards produce them.
        var modules = Enumerable.Range(0, Modules).Select(_ => Guid.NewGuid()).ToArray();
        var features = modules.ToDictionary(_ => _, _ => Enumerable.Range(0, FeaturesPerModule).Select(_ => Guid.NewGuid()).ToArray());
        var slices = features.Values.SelectMany(_ => _).ToDictionary(_ => _, _ => Enumerable.Range(0, SlicesPerFeature).Select(_ => Guid.NewGuid()).ToArray());

        var last = EventSequenceNumber.Unavailable;
        foreach (var module in modules)
        {
            last = (await eventStore.EventLog.Append(module, new HierarchyModuleAdded($"Module {module}"))).SequenceNumber;
        }

        for (var featureIndex = 0; featureIndex < FeaturesPerModule; featureIndex++)
        {
            foreach (var module in modules)
            {
                var feature = features[module][featureIndex];
                last = (await eventStore.EventLog.Append(feature, new HierarchyFeatureAdded(module, feature, $"Feature {feature}"))).SequenceNumber;
            }
        }

        for (var sliceIndex = 0; sliceIndex < SlicesPerFeature; sliceIndex++)
        {
            foreach (var feature in slices.Keys)
            {
                var slice = slices[feature][sliceIndex];
                last = (await eventStore.EventLog.Append(slice, new HierarchySliceAdded(feature, slice, $"Slice {slice}"))).SequenceNumber;
            }
        }

        for (var eventIndex = 0; eventIndex < EventsPerSlice; eventIndex++)
        {
            foreach (var slice in slices.Values.SelectMany(_ => _))
            {
                last = (await eventStore.EventLog.Append(slice, new HierarchyEventAdded(slice, Guid.NewGuid(), $"Event {eventIndex}"))).SequenceNumber;
            }
        }

        return (modules, last);
    }
}
