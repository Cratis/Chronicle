// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider.fallback_context;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_refreshing_collection_fallbacks : Specification
{
    readonly fallback_context _synthesized = new();
    readonly fallback_context _instance = new();
    readonly fallback_context _factory = new();
    readonly fallback_context _open = new();
    readonly List<Exception?> _rejections = [];
    readonly List<object> _collections = [];
    helper[] _currentCollection;
    generic<int>[] _currentGenericCollection;
    helper _expectedDirect;
    helper _direct;
    object _scopedGenericCollection;
    generic<int> _genericDirect;

    void Establish()
    {
        _synthesized.Build((services, _) => services.AddTransient<helper>());
        _instance.Build((services, first) => services.AddSingleton<IEnumerable<helper>>([new helper(first)]));
        _factory.Build((services, first) => services.AddSingleton<IEnumerable<helper>>(_ => [new helper(first)]));
        _open.Build((services, _) => services.AddTransient(typeof(generic<>), typeof(dependent_generic<>)));
        foreach (var context in Contexts())
        {
            context.Advance();
            context.Registry.Update([ServiceDescriptor.Singleton(new helper(context.Current))]);
        }
        _expectedDirect = _synthesized.Provider.GetRequiredService<helper>();
        _currentCollection = [new helper(_synthesized.Current)];
        _currentGenericCollection = [new generic<int>()];
        _open.Advance();
        _open.Registry.Update([ServiceDescriptor.Singleton(_currentGenericCollection[0])]);
    }

    void Because()
    {
        _direct = _synthesized.Provider.GetRequiredService<helper>();
        foreach (var context in Contexts())
        {
            _rejections.Add(Catch.Exception(() => context.Provider.GetServices<helper>()));
            using var scope = context.Provider.CreateScope();
            using var nested = scope.ServiceProvider.CreateScope();
            _rejections.Add(Catch.Exception(() => nested.ServiceProvider.GetServices<helper>()));
            context.Registry.Update([ServiceDescriptor.Singleton<IEnumerable<helper>>(_currentCollection)]);
            _collections.Add(context.Provider.GetServices<helper>());
            _collections.Add(nested.ServiceProvider.GetServices<helper>());
            context.Registry.Clear();
            _rejections.Add(Catch.Exception(() => nested.ServiceProvider.GetServices<helper>()));
        }
        _genericDirect = _open.Provider.GetRequiredService<generic<int>>();
        _rejections.Add(Catch.Exception(() => _open.Provider.GetServices<generic<int>>()));
        _open.Registry.Update([ServiceDescriptor.Singleton<IEnumerable<generic<int>>>(_currentGenericCollection)]);
        _collections.Add(_open.Provider.GetServices<generic<int>>());
        using var genericScope = _open.Provider.CreateScope();
        _scopedGenericCollection = genericScope.ServiceProvider.GetServices<generic<int>>();
    }

    void Destroy()
    {
        foreach (var context in Contexts()) context.Dispose();
        _open.Dispose();
    }

    [Fact] void should_resolve_the_current_element_directly() => _direct.ShouldBeSame(_expectedDirect);
    [Fact] void should_reject_all_stale_collections_even_with_current_elements() => Assert.All(_rejections, error => error.ShouldBeOfExactType<InvalidOperationException>());
    [Fact] void should_preserve_explicit_current_collection_identity_directly_and_in_nested_scopes() => Assert.All(_collections.Take(6), collection => collection.ShouldBeSame(_currentCollection));
    [Fact] void should_preserve_current_closed_element_identity() => _genericDirect.ShouldBeSame(_currentGenericCollection[0]);
    [Fact] void should_preserve_current_closed_collection_identity() => _collections[^1].ShouldBeSame(_currentGenericCollection);
    [Fact] void should_preserve_current_closed_collection_identity_in_a_scope() => _scopedGenericCollection.ShouldBeSame(_currentGenericCollection);
    [Fact] void should_not_construct_a_synthesized_stale_helper() => _synthesized.First.Constructions.ShouldEqual(0);
    [Fact] void should_not_reconstruct_the_explicit_inner_collection_instance() => _instance.First.Constructions.ShouldEqual(1);
    [Fact] void should_not_invoke_the_explicit_inner_collection_factory() => _factory.First.Constructions.ShouldEqual(0);
    [Fact] void should_not_construct_an_open_generic_from_the_first_context() => _open.First.Constructions.ShouldEqual(0);

    fallback_context[] Contexts() => [_synthesized, _instance, _factory];
}
