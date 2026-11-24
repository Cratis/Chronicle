// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.when_the_definition_changes.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing;

/// <summary>
/// Verifies that changing the definition of a projection active in several namespaces updates every one of them, with their data kept apart.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.ProjectionPlanSharingCollection.Name)]
public class when_the_definition_changes(context context) : Given<context>(context)
{
    public class context(given.ProjectionPlanSharingFixture fixture) : given.a_projection_in_many_namespaces(fixture)
    {
        public Dictionary<string, given.SharedOrder> Changed { get; } = [];
        public Dictionary<string, given.SharedOrder> Replayed { get; } = [];
        public Dictionary<string, given.SharedOrder> Live { get; } = [];

        async Task Because()
        {
            await ChangeDefinition();

            // The changed definition is used for the events that come after it...
            foreach (var @namespace in Namespaces)
            {
                (await _stores[@namespace].EventLog.Append("order-2", new given.SharedOrderPlaced(CustomerId, LabelIn(@namespace)))).IsSuccess.ShouldBeTrue();
                Changed[@namespace] = await WaitForOrder(@namespace, order => order.Summary == LabelIn(@namespace), "order-2");
            }

            // ...and reaches the events projected before it once the namespaces are replayed.
            await ReplayEveryNamespace();
            foreach (var @namespace in Namespaces)
            {
                Replayed[@namespace] = await WaitForOrder(@namespace, order => order.Summary == LabelIn(@namespace) && IsComplete(order, @namespace));
            }

            // ...and the order projected before it keeps being updated. it keeps being updated.
            foreach (var @namespace in Namespaces)
            {
                var sku = SkuIn(@namespace, 99);
                (await _stores[@namespace].EventLog.Append(OrderId, new given.SharedLineAdded(OrderId, sku, 99))).IsSuccess.ShouldBeTrue();
                Live[@namespace] = await WaitForOrder(@namespace, order => order.Lines.Any(line => line.Sku == sku));
            }

            Plans = PlansHeld;
        }

        public int Plans { get; private set; }
        public bool AppliedInEveryNamespace => Namespaces.All(@namespace => Changed[@namespace].Summary == LabelIn(@namespace));
        public bool ReplayedEveryNamespace => Namespaces.All(@namespace => Replayed[@namespace].Summary == LabelIn(@namespace));
        public bool UpdatedEveryNamespaceLive => Namespaces.All(@namespace =>
            Live[@namespace].Lines.Count() == LinesIn(@namespace) + 1 &&
            Live[@namespace].Lines.All(line => line.Sku.StartsWith(@namespace, StringComparison.Ordinal)));
    }

    [Fact] void should_apply_the_changed_definition_to_new_events_in_every_namespace() => Context.AppliedInEveryNamespace.ShouldBeTrue();
    [Fact] void should_apply_the_changed_definition_to_existing_events_in_every_namespace_when_replayed() => Context.ReplayedEveryNamespace.ShouldBeTrue();
    [Fact] void should_hold_only_the_current_and_the_previous_plan_for_all_namespaces() => Context.Plans.ShouldEqual(2);
    [Fact] void should_keep_projecting_new_events_in_every_namespace_without_mixing_them() => Context.UpdatedEveryNamespaceLive.ShouldBeTrue();
}
