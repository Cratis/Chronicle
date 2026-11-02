// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.when_a_definition_change_activates_a_subscriber.context;
using KernelProjectionResult = KernelCore::Cratis.Chronicle.Projections.ProjectionResult;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection;

/// <summary>
/// Verifies that a definition change does not wait for a subscriber it reactivates, which subscribes while activating.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.SubscriberLifecycleCollection.Name)]
public class when_a_definition_change_activates_a_subscriber(context context) : Given<context>(context)
{
    /// <summary>
    /// Deactivates the subscriber while it stays subscribed, then changes the definition.
    /// </summary>
    public class context(given.SubscriberLifecycleFixture fixture) : given.a_subscribed_immediate_projection(fixture)
    {
        /// <summary>
        /// Gets the position at which the definition change completed.
        /// </summary>
        public int ChangeCompletedOrder { get; private set; }

        /// <summary>
        /// Gets the subsequent read.
        /// </summary>
        public KernelProjectionResult Subsequent { get; private set; }

        /// <summary>
        /// Gets the position at which the reactivated subscriber subscribed.
        /// </summary>
        public int ResubscribeCompletedOrder => _gate.ResubscribeCompletedOrder;

        async Task Because()
        {
            _gate.Arm(dropUnsubscribe: true);
            await _immediateProjection.Dehydrate();
            await _gate.UnsubscribeEntered.WaitAsync(TimeSpan.FromSeconds(30));
            await _projection.SetDefinition(_changed).WaitAsync(TimeSpan.FromSeconds(60));
            ChangeCompletedOrder = _gate.NextOrder();
            Subsequent = await _immediateProjection.GetModelInstance().WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_subscribe_the_activated_subscriber_before_the_change_completes() => (Context.ResubscribeCompletedOrder > 0 && Context.ResubscribeCompletedOrder < Context.ChangeCompletedOrder).ShouldBeTrue();
    [Fact] void should_use_the_changed_definition_on_the_next_read() => Context.Subsequent.ReadModel["seed"]!.GetValue<string>().ShouldEqual("changed");
}
