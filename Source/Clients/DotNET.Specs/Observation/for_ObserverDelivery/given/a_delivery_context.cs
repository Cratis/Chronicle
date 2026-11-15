// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDelivery.given;

public class a_delivery_context : Specification
{
    protected ObserverDeliveryContext _context;
    protected List<string> _log;

    void Establish()
    {
        _context = new("store", "namespace-x", new("observer"), ObserverDeliveryKind.Reactor, typeof(a_delivery_context));
        _log = [];
    }

    protected IObserverDeliveryScope RecordingScope(string name, Action<ObserverDeliveryContext>? onEnter = null)
    {
        var scope = Substitute.For<IObserverDeliveryScope>();
        scope.Enter(Arg.Any<ObserverDeliveryContext>()).Returns(_ =>
        {
            _log.Add($"enter:{name}");
            onEnter?.Invoke(_.Arg<ObserverDeliveryContext>());
            return new ActionDisposable(() => _log.Add($"leave:{name}"));
        });
        return scope;
    }

    protected IServiceProvider ProviderOf(params IObserverDeliveryScope[] scopes)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IEnumerable<IObserverDeliveryScope>)).Returns(scopes);
        return provider;
    }

    protected sealed class ActionDisposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
