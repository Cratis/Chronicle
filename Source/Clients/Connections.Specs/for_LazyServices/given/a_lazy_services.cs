// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Grpc.Core;

namespace Cratis.Chronicle.Connections.for_LazyServices.given;

public class a_lazy_services : Specification
{
    protected CallInvoker _callInvoker;
    protected recording_client_factory _factory;
    protected IServices _services;

    void Establish()
    {
        _callInvoker = Substitute.For<CallInvoker>();
        _factory = new recording_client_factory();
        _services = new LazyServices(_callInvoker, _factory);
    }
}
