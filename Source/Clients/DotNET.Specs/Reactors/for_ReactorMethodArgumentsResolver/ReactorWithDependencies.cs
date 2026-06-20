// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver;

public class ReactorWithDependencies : IReactor
{
    public Task EventOnly(SomeEvent @event) => Task.CompletedTask;

    public Task EventAndContext(SomeEvent @event, EventContext context) => Task.CompletedTask;

    public Task WithReadModel(SomeEvent @event, SomeReadModel readModel) => Task.CompletedTask;

    public Task WithService(SomeEvent @event, ISomeService service) => Task.CompletedTask;

    public Task Mixed(SomeEvent @event, EventContext context, SomeReadModel readModel, ISomeService service) => Task.CompletedTask;
}
