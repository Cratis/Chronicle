// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionedDeliveryScheduler.when_scheduling;

public class and_the_previous_work_entered_a_delivery : Specification
{
    static readonly AsyncLocal<string?> _hookState = new();
    PartitionedDeliveryScheduler _scheduler;
    ObserverDeliveryContext? _contextSeenBySecond;
    string? _hookStateSeenBySecond;

    void Establish() => _scheduler = new(2);

    async Task Because()
    {
        var hook = new SettingHook();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IEnumerable<IObserverDeliveryScope>)).Returns(new IObserverDeliveryScope[] { hook });

        await _scheduler.Schedule("partition", () =>
        {
            var delivery = ObserverDeliveryScope.Enter(provider, new("store", "namespace-x", new("observer"), ObserverDeliveryKind.Reactor, typeof(object)));
            delivery.Dispose();
            return Task.CompletedTask;
        }).WaitAsync(TimeSpan.FromSeconds(10));

        await _scheduler.Schedule("partition", () =>
        {
            _contextSeenBySecond = ObserverDeliveryContext.Current;
            _hookStateSeenBySecond = _hookState.Value;
            return Task.CompletedTask;
        }).WaitAsync(TimeSpan.FromSeconds(10));
    }

    void Destroy() => _scheduler.Dispose();

    [Fact] void should_not_carry_the_delivery_context_to_the_next_work() => _contextSeenBySecond.ShouldBeNull();
    [Fact] void should_not_carry_state_set_by_a_scope_to_the_next_work() => _hookStateSeenBySecond.ShouldBeNull();

    sealed class SettingHook : IObserverDeliveryScope
    {
        public IDisposable Enter(ObserverDeliveryContext context)
        {
            _hookState.Value = "entered";
            return new Clearing();
        }

        sealed class Clearing : IDisposable
        {
            public void Dispose() => _hookState.Value = null;
        }
    }
}
