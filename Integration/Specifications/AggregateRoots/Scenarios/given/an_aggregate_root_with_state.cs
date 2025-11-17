// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots.Domain;
using Cratis.Chronicle.Integration.Specifications.AggregateRoots.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.Specifications.AggregateRoots.Scenarios.given;

public class an_aggregate_root_with_state<TAggregate, TInternalState>(IChronicleFixture chronicleFixture) : Specification<IChronicleFixture>(chronicleFixture)
    where TAggregate : IAggregateRoot
    where TInternalState : class
{
#pragma warning disable CA2213 // Disposable fields should be disposed
    protected IChronicleFixture ChronicleFixture = chronicleFixture;
#pragma warning restore CA2213 // Disposable fields should be disposed

    public TInternalState ResultState;
    public override IEnumerable<Type> AggregateRoots => [typeof(User)];
    public override IEnumerable<Type> EventTypes => [typeof(UserOnBoarded), typeof(UserCreated), typeof(UserDeleted), typeof(UserNameChanged)];

    protected List<EventAndEventSourceId> EventsWithEventSourceIdToAppend = [];
    protected IAggregateRootFactory AggregateRootFactory => Services.GetRequiredService<IAggregateRootFactory>();
    protected IUnitOfWorkManager UnitOfWorkManager => Services.GetRequiredService<IUnitOfWorkManager>();

    protected override void ConfigureServices(IServiceCollection services)
    {
    }

    protected async Task DoOnAggregate(EventSourceId eventSourceId, Func<TAggregate, Task> action, bool commitUnitOfWork = true)
    {
        var user = await AggregateRootFactory.Get<TAggregate>(eventSourceId);
        await action(user);
        await user.Commit();
    }

    void Establish()
    {
    }

    async Task Because()
    {
        foreach (var @event in EventsWithEventSourceIdToAppend)
        {
            await EventStore.EventLog.Append(@event.EventSourceId, @event.Event);
        }
    }
}
