// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.given;

public class an_ensure_command : Specification
{
    protected static readonly EventStoreName _store = "the-store";
    protected static readonly EventStoreNamespaceName _namespace = "new-tenant";
    protected IGrainFactory _grains;
    protected INamespaces _namespaces;
    protected IProjectionsManager _projections;
    protected ICapturesManager _captures;
    protected EnsureNamespace _command;

    void Establish()
    {
        _grains = Substitute.For<IGrainFactory>();
        _namespaces = Substitute.For<INamespaces>();
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _grains.GetGrain<INamespaces>(_store.Value).Returns(_namespaces);
        _grains.GetGrain<IProjectionsManager>(_store.Value).Returns(_projections);
        _grains.GetGrain<ICapturesManager>(_store.Value).Returns(_captures);
        _command = new(_store, _namespace);
    }
}
