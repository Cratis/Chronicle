// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorInvoker.when_invoking;

public class the_added_method : given.an_invoker
{
    WatchedReadModel _model;

    async Task Because()
    {
        _model = new WatchedReadModel();
        await _invoker.Invoke(_eventStore, _reactor, MethodFor(ReadModelChangeType.Added), _model, _changeContext, _serviceProvider);
    }

    [Fact] void should_pass_the_read_model() => _reactor.AddedModel.ShouldEqual(_model);
    [Fact] void should_pass_the_change_context() => _reactor.AddedContext.ShouldEqual(_changeContext);
}
