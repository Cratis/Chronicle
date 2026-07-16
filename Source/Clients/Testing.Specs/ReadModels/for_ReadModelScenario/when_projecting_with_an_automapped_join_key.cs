// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Regression for CHR-14: a <c>[Join]</c> whose <c>on</c> column is populated by AutoMap (a name-matched
/// foreign key with NO <c>[SetFrom]</c>) must be backfilled at row-creation time (Path B / ResolveJoin)
/// against the joining row's OWN join-source event — even when the join-source events already exist because
/// they were seeded BEFORE the row-creating from-events (the common production order).
/// <para>
/// Two distinct customers are seeded first, then two orders — one per customer. Before the fix, the
/// row-creation-time backfill is only wired when the join key appears in the explicit From mappings, so an
/// AutoMapped join key never resolves its own source at row creation: each order surfaces whatever customer
/// name the harness carried over from the last-processed join source (here, the OTHER customer), not its
/// own. Asserting each order shows ITS OWN customer's name therefore fails without the fix and passes with it.
/// </para>
/// </summary>
public class when_projecting_with_an_automapped_join_key : Specification
{
    ReadModelScenario<AutoMappedJoinOrderSummary> _scenario;
    Guid _xavierGuid;
    Guid _yolandaGuid;
    EventSourceId _orderForXavierId;
    EventSourceId _orderForYolandaId;
    Guid _orderForXavierGuid;
    Guid _orderForYolandaGuid;
    AutoMappedJoinOrderSummary? _orderForXavier;
    AutoMappedJoinOrderSummary? _orderForYolanda;

    void Establish()
    {
        _scenario = new ReadModelScenario<AutoMappedJoinOrderSummary>();
        _xavierGuid = Guid.NewGuid();
        _yolandaGuid = Guid.NewGuid();
        _orderForXavierGuid = Guid.NewGuid();
        _orderForYolandaGuid = Guid.NewGuid();
        _orderForXavierId = new EventSourceId(_orderForXavierGuid);
        _orderForYolandaId = new EventSourceId(_orderForYolandaGuid);
    }

    async Task Because()
    {
        // Seed both JOIN SOURCES (customers) first — the common production order where the join sources
        // already exist when the order rows are created. Each order's AutoMapped join key must resolve its
        // OWN customer, not the last one processed.
        await _scenario.Given
            .ForEventSource(new EventSourceId(_xavierGuid))
            .Events(new JoinCustomerRegistered("Xavier"));

        await _scenario.Given
            .ForEventSource(new EventSourceId(_yolandaGuid))
            .Events(new JoinCustomerRegistered("Yolanda"));

        await _scenario.Given
            .ForEventSource(_orderForXavierId)
            .Events(new JoinOrderPlaced(new JoinCustomerId(_xavierGuid), 100m));

        await _scenario.Given
            .ForEventSource(_orderForYolandaId)
            .Events(new JoinOrderPlaced(new JoinCustomerId(_yolandaGuid), 200m));

        _orderForXavier = _scenario.InstanceForEventSourceId(_orderForXavierId);
        _orderForYolanda = _scenario.InstanceForEventSourceId(_orderForYolandaId);
    }

    [Fact] void should_resolve_the_order_for_xavier() => _orderForXavier.ShouldNotBeNull();
    [Fact] void should_resolve_the_order_for_yolanda() => _orderForYolanda.ShouldNotBeNull();
    [Fact] void should_key_each_order_by_its_own_id() => (_orderForXavier!.Id, _orderForYolanda!.Id).ShouldEqual((_orderForXavierGuid, _orderForYolandaGuid));
    [Fact] void should_have_each_amount() => (_orderForXavier!.Amount, _orderForYolanda!.Amount).ShouldEqual((100m, 200m));
    [Fact] void should_join_xaviers_name_onto_his_order() => _orderForXavier!.CustomerName.ShouldEqual("Xavier");
    [Fact] void should_join_yolandas_name_onto_her_order() => _orderForYolanda!.CustomerName.ShouldEqual("Yolanda");
}
