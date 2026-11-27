// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.for_IObserver;

public class when_declaring_the_handover_repair_acknowledgement : Specification
{
    MethodInfo _acknowledgement;

    void Because() => _acknowledgement = typeof(IObserver).GetMethod(nameof(IObserver.HandoverRepairCaughtUp))!;

    [Fact] void should_declare_the_callback_on_interface_version_thirteen() => _acknowledgement.DeclaringType!.GetCustomAttribute<VersionAttribute>()!.Version.ShouldEqual((ushort)13);
    [Fact] void should_allow_the_callback_during_the_turn_resuming_its_job() => _acknowledgement.IsDefined(typeof(AlwaysInterleaveAttribute)).ShouldBeTrue();
}
