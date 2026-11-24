// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections;
using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.when_the_projection_is_removed.context;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing;

/// <summary>
/// Verifies that dropping a projection from the registered set stops it in every namespace it was active in.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.ProjectionPlanSharingCollection.Name)]
public class when_the_projection_is_removed(context context) : Given<context>(context)
{
    public class context(given.ProjectionPlanSharingFixture fixture) : given.a_projection_in_many_namespaces(fixture)
    {
        public bool StillDefined { get; private set; }
        public int Plans { get; private set; }
        public Dictionary<string, string> After { get; } = [];

        async Task Because()
        {
            var manager = _grains.GetGrain<IProjectionsManager>(_storeName);

            // The application deploys a full set of projections that no longer contains it.
            await manager.Register([], ProjectionOwner.Client);
            Plans = PlansHeld;
            StillDefined = (await manager.GetProjectionDefinitions()).Any(definition => definition.Identifier.Value == _projectionId);

            foreach (var @namespace in Namespaces)
            {
                (await _stores[@namespace].EventLog.Append(OrderId, new given.SharedLineAdded(OrderId, SkuIn(@namespace, 99), 99))).IsSuccess.ShouldBeTrue();
            }

            // Nothing is left to project it, and the public read refuses a read model with no producer, so read the stored order from the sink; give a surviving observer ample time to prove it wrong.
            // Task.Delay: proving an absence; there is no fact to await that something did not happen.
            await Task.Delay(TimeSpan.FromSeconds(5));
            foreach (var @namespace in Namespaces)
            {
                After[@namespace] = await StoredOrderIn(@namespace);
            }
        }

        public bool NoNamespaceProjectedTheLaterEvent => Namespaces.All(@namespace =>
            After[@namespace].Contains(SkuIn(@namespace, LinesIn(@namespace)), StringComparison.Ordinal) &&
            !After[@namespace].Contains(SkuIn(@namespace, 99), StringComparison.Ordinal));
    }

    [Fact] void should_no_longer_define_the_projection() => Context.StillDefined.ShouldBeFalse();
    [Fact] void should_evict_the_compiled_plan() => Context.Plans.ShouldEqual(0);
    [Fact] void should_stop_projecting_in_every_namespace() => Context.NoNamespaceProjectedTheLaterEvent.ShouldBeTrue();
}
