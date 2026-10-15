// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_EventStreamReadModel.given;

public class stream_models : Specification
{
    protected IEventTypes _types;
    internal ModelBoundProjectionBuilder _modelBound;

    [EventSource("BankAccount")]
    [EventSources.EventStream("Transactions")]
    protected class BankAccount : IEventSource;

    [EventType]
    protected record MoneyWithdrawn(decimal Amount);

    [EventStream<BankAccount>("Transactions")]
    protected record MonthlyWithdrawals(string Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Withdrawn);

    [EventStream<BankAccount>("Missing")]
    [FromEvent<MoneyWithdrawn>]
    protected record UndeclaredStream(string Id);

    [EventStream<BankAccount>("All")]
    [FromEvent<MoneyWithdrawn>]
    protected record AllStreams(string Id);

    [FromEvent<MoneyWithdrawn>]
    protected record ExistingModel(string Id);

    void Establish()
    {
        _types = Substitute.For<IEventTypes>();
        _types.GetEventTypeFor(typeof(MoneyWithdrawn)).Returns(new EventType("money-withdrawn", 1));
        _modelBound = new ModelBoundProjectionBuilder(new CamelCaseNamingPolicy(), _types);
    }

    protected ProjectionBuilderFor<T> Fluent<T>() => new("projection", typeof(T), new CamelCaseNamingPolicy(), _types, new JsonSerializerOptions());
}
