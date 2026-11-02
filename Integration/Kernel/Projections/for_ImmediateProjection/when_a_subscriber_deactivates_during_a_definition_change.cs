// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection.when_a_subscriber_deactivates_during_a_definition_change.context;
using KernelProjectionResult = KernelCore::Cratis.Chronicle.Projections.ProjectionResult;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ImmediateProjection;

/// <summary>
/// Verifies that a definition change does not wait for a subscriber that is unsubscribing while it deactivates.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.SubscriberLifecycleCollection.Name)]
public class when_a_subscriber_deactivates_during_a_definition_change(context context) : Given<context>(context)
{
    /// <summary>
    /// Holds the subscriber's unsubscribe while the definition changes.
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
        /// Gets the position at which the held unsubscribe completed.
        /// </summary>
        public int UnsubscribeCompletedOrder => _gate.UnsubscribeCompletedOrder;

        /// <summary>
        /// Gets the position at which the reactivated subscriber subscribed again.
        /// </summary>
        public int ResubscribeCompletedOrder => _gate.ResubscribeCompletedOrder;

        async Task Because()
        {
            _gate.Arm(dropUnsubscribe: false);
            try
            {
                await _immediateProjection.Dehydrate();
                await _gate.UnsubscribeEntered.WaitAsync(TimeSpan.FromSeconds(30));
                var change = _projection.SetDefinition(_changed);
                await _gate.NotificationSent.WaitAsync(TimeSpan.FromSeconds(30));
                _gate.ReleaseUnsubscribe();
                await change.WaitAsync(TimeSpan.FromSeconds(60));
                ChangeCompletedOrder = _gate.NextOrder();
            }
            finally
            {
                _gate.ReleaseUnsubscribe();
            }

            Subsequent = await _immediateProjection.GetModelInstance().WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_unsubscribe_before_the_definition_change_completes() => (Context.UnsubscribeCompletedOrder > 0 && Context.UnsubscribeCompletedOrder < Context.ChangeCompletedOrder).ShouldBeTrue();
    [Fact] void should_resubscribe_the_reactivated_subscriber_during_the_change() => (Context.ResubscribeCompletedOrder > 0 && Context.ResubscribeCompletedOrder < Context.ChangeCompletedOrder).ShouldBeTrue();
    [Fact] void should_use_the_changed_definition_on_the_next_read() => Context.Subsequent.ReadModel["seed"]!.GetValue<string>().ShouldEqual("changed");
}
