// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ReadProtectionMerge.given;

public class contributions : Specification
{
    internal ReadProtectionTarget _target;
    internal List<ReadProtectionContribution> _contributions;
    protected Dictionary<EventSourceId, ConcurrencyScope> _legacy;
    internal ReadProtectionPlan _plan;
    protected Exception _error;
    protected EventSourceId _label;
    protected EventType _stockReduced;
    protected EventType _stockRemoved;

    void Establish()
    {
        _target = new("store", "tenant", EventSequenceId.Log);
        _label = "stock";
        _stockReduced = new("stock-reduced", EventTypeGeneration.First);
        _stockRemoved = new("stock-removed", EventTypeGeneration.First);
        _contributions = [];
        _legacy = [];
    }

    internal void Add(ReadProtectionContributionKind kind, ConcurrencyScope scope, string description = "Stock", ReadProtectionContributionIntent intent = ReadProtectionContributionIntent.None) =>
        _contributions.Add(new(_label, kind, scope, _target, description, intent));

    protected void Read(EventSequenceNumber boundary) => Add(ReadProtectionContributionKind.ProtectedRead, new(boundary, _label, EventTypes: [_stockReduced]));

    internal ReadProtectionPlan Merge(bool hasEvents = true, bool supportsConjunctions = true) => ReadProtectionMerge.Apply(_target, _contributions, _legacy, hasEvents, supportsConjunctions: supportsConjunctions);
}
