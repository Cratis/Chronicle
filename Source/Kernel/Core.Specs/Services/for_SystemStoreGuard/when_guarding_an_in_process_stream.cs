// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Services.for_SystemStoreGuard;

public class when_guarding_an_in_process_stream : Specification
{
    readonly IObservable<int> _source = Observable.Never<int>();
    IServiceProvider _previous;
    IObservable<int> _result;

    void Establish()
    {
        _previous = SystemStoreGuard.InProcessServices;
        SystemStoreGuard.InProcessServices = null;
    }

    void Because() => _result = SystemStoreGuard.Ensure(default, new("store", "ns"), StoreAccess.Read, () => _source);

    void Destroy() => SystemStoreGuard.InProcessServices = _previous;

    [Fact] void should_keep_the_original_stream() => ReferenceEquals(_result, _source).ShouldBeTrue();
}
