// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlProviderHost;

/// <summary>Verifies constraint ownership and isolation through the benchmark SQL adapter.</summary>
public class when_using_constraints : Specification
{
    SqlProviderHost _host = null!;
    bool _ownerAllowed;
    bool _otherAllowed;
    bool _isolated;
    ulong _sequence;
    int _values;

    async Task Establish()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
    }

    async Task Because()
    {
        var (storage, definition) = await _host.CreateConstraints();
        await storage.Save("owner", definition, 12, "value");
        (_ownerAllowed, _) = await storage.IsAllowed("owner", definition, "value");
        var other = await storage.IsAllowed("other", definition, "value");
        _otherAllowed = other.IsAllowed;
        _sequence = other.SequenceNumber;
        _values = (await storage.GetFor("owner", definition)).Count();
        var (isolated, isolatedDefinition) = await _host.CreateConstraints();
        (_isolated, _) = await isolated.IsAllowed("other", isolatedDefinition, "value");
    }

    async Task Destroy() => await _host.DisposeAsync();

    [Fact] void should_allow_the_owner() => _ownerAllowed.ShouldBeTrue();
    [Fact] void should_reject_another_owner() => _otherAllowed.ShouldBeFalse();
    [Fact] void should_preserve_sequence_number() => _sequence.ShouldEqual(12UL);
    [Fact] void should_round_trip_the_value() => _values.ShouldEqual(1);
    [Fact] void should_isolate_constraints() => _isolated.ShouldBeTrue();
}
