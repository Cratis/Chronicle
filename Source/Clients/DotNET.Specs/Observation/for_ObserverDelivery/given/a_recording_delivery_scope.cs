// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery.given;

/// <summary>
/// An <see cref="IObserverDeliveryScope"/> that records entering and leaving into a shared log.
/// </summary>
/// <param name="log">The shared log.</param>
/// <param name="left">Optional callback invoked after a scope has been left.</param>
public class a_recording_delivery_scope(List<string> log, Action? left = null) : IObserverDeliveryScope
{
    /// <inheritdoc/>
    public IDisposable Enter(ObserverDeliveryContext context)
    {
        log.Add($"enter:{context.Namespace}");
        return new Leaving(log, left);
    }

    sealed class Leaving(List<string> log, Action? left) : IDisposable
    {
        public void Dispose()
        {
            log.Add("leave");
            left?.Invoke();
        }
    }
}
