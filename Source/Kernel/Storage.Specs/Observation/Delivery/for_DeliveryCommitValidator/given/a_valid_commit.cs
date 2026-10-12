// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryCommitValidator.given;

public class a_valid_commit : Specification
{
    protected ObserverDeliveryRoot _root;
    protected ParkedKey _parked;
    protected SkipRange _skip;
    protected DeliveryCommit _commit;

    void Establish()
    {
        _root = new("observer", DeliveryRootFormat.Current, DeliveryLifecycle.Engine, new(0), 0, 0, [new(new(0), new(0), new(1), 0)], new(0), 0, new(0), null, null, new(1), new(Guid.NewGuid()));
        var key = SequencingKeyIdentity.For(new ValueSequencingKey(["key"]));
        _parked = new(_root.Observer, new(0), key, new(0), ParkedKeyKind.Handling, 0, 0, new(0), false, _root.Revision, null);
        _skip = new(_root.Observer, new(0), key, new(0), new(1), SkipReason.Operator, DateTimeOffset.UnixEpoch, _root.Revision);
        _commit = Create(_root, [ParkedKeyOperation.Insert(_parked)], [SkipRangeOperation.Insert(_skip)]);
    }

    protected static ObserverDeliveryRoot WithLanes(ObserverDeliveryRoot root, ImmutableArray<DeliveryLane> lanes) =>
        new(root.Observer, root.Format, root.Lifecycle, root.Generation, root.KeyDefinitionVersion, root.MapVersion, lanes, root.ActivationEpoch, root.ScheduleClock, root.ScheduleTicket, root.CoverageHead, root.Operation, root.Revision, root.LastCommit);

    protected static DeliveryCommit Create(ObserverDeliveryRoot root, ImmutableArray<ParkedKeyOperation> parked, ImmutableArray<SkipRangeOperation> skips) =>
        new(DeliveryRevision.None, root, parked, skips);
}
