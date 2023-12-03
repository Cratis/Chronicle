// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Specifications;

namespace Aksio.Cratis.Reducers.for_ReducerInvoker.given;

public class a_reducer_invoker_for<TReducer> : Specification
    where TReducer : class, new()
{
    protected ReducerInvoker invoker;
    protected Mock<IServiceProvider> service_provider;
    protected IEventTypes event_types;
    protected TReducer reducer;
    protected EventType event_type;

    void Establish()
    {
        reducer = new();
        service_provider = new();
        service_provider.Setup(_ => _.GetService(typeof(TReducer))).Returns(reducer);
        event_types = new EventTypesForSpecifications(GetEventTypes());
        event_type = new(Guid.Parse("d22efe41-41c6-408e-b5d2-c0d54757cbf8"), 1);
        invoker = new ReducerInvoker(
            service_provider.Object,
            event_types,
            typeof(TReducer),
            typeof(ReadModel));
    }

    protected virtual IEnumerable<Type> GetEventTypes() => Enumerable.Empty<Type>();
}
