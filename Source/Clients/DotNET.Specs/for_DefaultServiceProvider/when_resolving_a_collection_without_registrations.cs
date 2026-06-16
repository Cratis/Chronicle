// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_DefaultServiceProvider;

public class when_resolving_a_collection_without_registrations : Specification
{
    DefaultServiceProvider _provider;
    IEnumerable<IReactorSideEffectHandler> _result;

    void Establish() => _provider = new DefaultServiceProvider();

    void Because() => _result = _provider.GetServices<IReactorSideEffectHandler>();

    [Fact] void should_return_an_empty_collection() => _result.ShouldBeEmpty();
}
