// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.when_active_in_many_namespaces.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing;

/// <summary>
/// Verifies that the same projection, active in several namespaces, projects each namespace's own events into its own read model.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.ProjectionPlanSharingCollection.Name)]
public class when_active_in_many_namespaces(context context) : Given<context>(context)
{
    public class context(given.ProjectionPlanSharingFixture fixture) : given.a_projection_in_many_namespaces(fixture)
    {
        public Dictionary<string, given.SharedOrder> Orders { get; } = [];

        async Task Because()
        {
            foreach (var @namespace in Namespaces)
            {
                Orders[@namespace] = await WaitForOrder(@namespace, order => IsComplete(order, @namespace));
            }

            Plans = PlansHeld;
        }

        public int Plans { get; private set; }

        public bool Isolated => Namespaces.All(@namespace =>
            Orders[@namespace].Label == LabelIn(@namespace) &&
            Orders[@namespace].CustomerName == CustomerNameIn(@namespace) &&
            Orders[@namespace].Lines.Select(line => line.Sku).Order().SequenceEqual(Enumerable.Range(1, LinesIn(@namespace)).Select(line => SkuIn(@namespace, line))));
    }

    [Fact] void should_project_a_read_model_in_every_namespace() => Context.Orders.Count.ShouldEqual(given.a_projection_in_many_namespaces.Namespaces.Length);
    [Fact] void should_compile_the_projection_once_for_all_namespaces() => Context.Plans.ShouldEqual(1);
    [Fact] void should_keep_label_customer_name_and_children_of_each_namespace_apart() => Context.Isolated.ShouldBeTrue();
}
