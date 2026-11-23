// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization.Serializers;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers;

/// <summary>
/// The state a grain persists and the request and result of a job step are not arguments of a grain method, so the payload
/// manifest does not see them, yet each one reaches another silo or the storage of its grain.
/// </summary>
public class when_sending_the_state_and_job_step_types_between_silos : given.the_kernel_serializers
{
    List<string> _without_serializer;
    HandleEventsForPartitionResult _result;

    void Because()
    {
        var codecs = _provider.GetRequiredService<ICodecProvider>();
        _without_serializer = [.. JobAndStatePayloads.All
            .Where(type => !JsonSerializedTypes.Includes(type) && !HasCodec(codecs, type))
            .Select(type => type.FullName ?? type.Name)
            .Order()];

        _result = (HandleEventsForPartitionResult)SendBetweenSilos(
            typeof(HandleEventsForPartitionResult),
            new HandleEventsForPartitionResult(new EventSequenceNumber(42)) { LastScannedEventSequenceNumber = new EventSequenceNumber(44) })!;
    }

    [Fact] void should_find_the_types() => JobAndStatePayloads.All.Count.ShouldBeGreaterThan(20);
    [Fact] void should_have_a_serializer_for_every_type() => _without_serializer.ShouldBeEmpty();
    [Fact] void should_keep_the_last_handled_event_of_a_step_result() => _result.LastHandledEventSequenceNumber.ShouldEqual(new EventSequenceNumber(42));
    [Fact] void should_keep_the_last_scanned_event_of_a_step_result() => _result.LastScannedEventSequenceNumber.ShouldEqual(new EventSequenceNumber(44));

    static bool HasCodec(ICodecProvider codecs, Type type)
    {
        try
        {
            return codecs.GetType().GetMethod(nameof(CodecProvider.TryGetCodec), [typeof(Type)])?.Invoke(codecs, [type]) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
