// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery;

public class when_entering_without_any_scope_registered_repeatedly : given.a_delivery_context
{
    const int Iterations = 1000;
    static readonly AsyncLocal<ObserverDeliveryContext?> _baselineAmbient = new();
    long _baselineBytes;
    long _deliveryBytes;

    void Because()
    {
        var provider = new DefaultServiceProvider();

        // Warm up, so one-off caching and JIT work is not counted.
        for (var i = 0; i < 10; i++)
        {
            using var warm = ObserverDeliveryScope.Enter(provider, _context);
            _baselineAmbient.Value = _context;
            _baselineAmbient.Value = null;
        }

        // Writing an AsyncLocal allocates in the runtime itself; that is the floor the delivery shares.
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iterations; i++)
        {
            _baselineAmbient.Value = _context;
            _baselineAmbient.Value = null;
        }

        _baselineBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Iterations; i++)
        {
            using var delivery = ObserverDeliveryScope.Enter(provider, _context);
        }

        _deliveryBytes = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_not_allocate_beyond_publishing_the_ambient_context() => _deliveryBytes.ShouldBeLessThanOrEqual(_baselineBytes);
}
