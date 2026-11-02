// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider.fallback_context;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_explicit_closed_collections : Specification
{
    readonly fallback_context _instance = new();
    readonly fallback_context _factory = new();
    readonly List<Exception?> _errors = [];
    int _factoryCalls;

    void Establish()
    {
        _instance.Build((services, _) => services.AddSingleton<IEnumerable<generic<int>>>([new generic<int>()]));
        _factory.Build((services, _) => services.AddSingleton<IEnumerable<generic<int>>>(_ =>
        {
            _factoryCalls++;
            return [new generic<int>()];
        }));
        _instance.Advance();
        _factory.Advance();
    }

    void Because()
    {
        foreach (var context in new[] { _instance, _factory })
        {
            _errors.Add(Catch.Exception(() => context.Provider.GetServices<generic<int>>()));
            using var scope = context.Provider.CreateScope();
            _errors.Add(Catch.Exception(() => scope.ServiceProvider.GetServices<generic<int>>()));
        }
    }

    void Destroy()
    {
        _instance.Dispose();
        _factory.Dispose();
    }

    [Fact] void should_reject_explicit_collections_of_a_protected_closed_generic() => Assert.All(_errors, error => error.ShouldBeOfExactType<InvalidOperationException>());
    [Fact] void should_not_invoke_the_inner_collection_factory() => _factoryCalls.ShouldEqual(0);
}
