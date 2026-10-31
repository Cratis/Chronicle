// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Observation.for_Observer;

[Trait("Category", "GrainPayloadCompatibility")]
public class when_using_shadow_claims_as_storage_metadata : Specification
{
    Type[] _storageTypes;

    void Establish() => _storageTypes = [typeof(KernelObserverState), typeof(PartitionClaim), typeof(ObserverWideClaim), typeof(OwnershipRevision)];

    [Fact] void should_not_add_grain_serializers_to_storage_metadata() => _storageTypes
        .SelectMany(type => type.GetCustomAttributesData()).Any(attribute => attribute.AttributeType.Name == "GenerateSerializerAttribute").ShouldBeFalse();
    [Fact] void should_not_change_the_positional_state_contract() => typeof(KernelObserverState).GetConstructors()
        .Single(constructor => constructor.GetParameters().Length > 0).GetParameters().Select(parameter => parameter.Name)
        .ShouldContainOnly("Identifier", "LastHandledEventSequenceNumber", "RunningState", "ReplayingPartitions", "CatchingUpPartitions", "FailedPartitions", "FailedPartitionCount", "IsReplaying", "SubscribesToAllEvents");
    [Fact] void should_leave_empty_partition_claims_null() => KernelObserverState.Empty.PartitionClaims.ShouldBeNull();
    [Fact] void should_leave_empty_observer_wide_claim_null() => KernelObserverState.Empty.ObserverWideClaim.ShouldBeNull();
    [Fact] void should_leave_empty_revision_null() => KernelObserverState.Empty.OwnershipRevision.ShouldBeNull();
    [Fact] void should_not_add_field_ids_for_shadow_metadata() => typeof(KernelObserverState).GetProperties()
        .Where(property => string.Equals(property.Name, nameof(KernelObserverState.PartitionClaims), StringComparison.Ordinal) || string.Equals(property.Name, nameof(KernelObserverState.ObserverWideClaim), StringComparison.Ordinal) || string.Equals(property.Name, nameof(KernelObserverState.OwnershipRevision), StringComparison.Ordinal))
        .SelectMany(property => property.GetCustomAttributesData()).Any(attribute => attribute.AttributeType.Name == "IdAttribute").ShouldBeFalse();
}
