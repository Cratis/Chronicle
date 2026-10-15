// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing.given;

public class a_collection_stream : for_ReadModels.given.all_dependencies
{
    protected Subject<ObserveInstancesResponse> _stream;
    protected Contracts.ReadModels.IMaterializedReadModels _service;
    protected ObserveInstancesRequest _request;
    protected List<IEnumerable<Row>> _emissions;
    protected ISubject<IEnumerable<Row>> _observation;
    protected IDisposable _subscription;
    protected Exception _error;
    protected bool _disposed;
    protected SemaphoreSlim _signal;

    void Establish()
    {
        _projections.HasFor(Arg.Is<Type>(type => type == typeof(Row))).Returns(true);
        _stream = new();
        _signal = new(0);
        _emissions = [];
        _service = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _services.MaterializedReadModels.Returns(_service);
        _service.ObserveInstances(Arg.Any<ObserveInstancesRequest>()).Returns(call =>
        {
            _request = call.Arg<ObserveInstancesRequest>();
            return Observable.Create<ObserveInstancesResponse>(observer => new CompositeDisposable(_stream.Subscribe(observer), Disposable.Create(() => _disposed = true)));
        });
    }

    protected void Subscribe() => _subscription = _observation.Subscribe(
        set =>
        {
            _emissions.Add(set);
            _signal.Release();
        },
        error =>
        {
            _error = error;
            _signal.Release();
        });

    protected async Task Emit(params Row[] rows)
    {
        _stream.OnNext(new() { Instances = rows.Select(row => JsonSerializer.Serialize(row)).ToList() });
        (await _signal.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
    }

    void Destroy()
    {
        _subscription?.Dispose();
        (_observation as IDisposable)?.Dispose();
        _stream.Dispose();
        _signal.Dispose();
    }

    public record Row(string WorkItemId, string Name);
}
