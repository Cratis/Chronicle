// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using static Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider.fallback_context;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_selecting_container_fallbacks : Specification
{
    readonly fallback_context _unsafeFirst = new();
    readonly fallback_context _unsafeLast = new();
    readonly fallback_context _safeOnly = new();
    readonly fallback_context _unsafeOpen = new();
    readonly fallback_context _unsafeClosed = new();
    Exception? _unsafeFirstCollection;
    Exception? _unsafeLastSingle;
    Exception? _unsafeLastCollection;
    Exception? _unsafeOpenCollection;
    Exception? _unsafeOpenSingle;
    Exception? _unsafeClosedSingle;
    Exception? _unsafeClosedCollection;
    free _safeSingle;
    free[] _safeCollection;
    generic<int> _closedOverride;
    generic<string> _unrelated;

    void Establish()
    {
        _unsafeFirst.Build((services, _) => services.AddTransient<free>(sp =>
        {
            sp.GetRequiredService<tracker>().Constructions++;
            return new free();
        }).AddTransient<free>());
        _unsafeLast.Build((services, _) => services.AddTransient<free>().AddTransient<free>(sp =>
        {
            sp.GetRequiredService<tracker>().Constructions++;
            return new free();
        }));
        _safeOnly.Build((services, _) => services.AddSingleton<free>().AddTransient<free>());
        _unsafeOpen.Build((services, _) => services
            .AddTransient(typeof(generic<>), typeof(dependent_generic<>))
            .AddTransient<generic<int>>());
        _unsafeClosed.Build((services, _) => services.AddTransient(typeof(generic<>))
            .AddTransient<generic<int>>(sp => new dependent_generic<int>(sp.GetRequiredService<tracker>())));
        foreach (var context in Contexts()) context.Advance();
    }

    void Because()
    {
        _safeSingle = _unsafeFirst.Provider.GetRequiredService<free>();
        _unsafeFirstCollection = Catch.Exception(() => _unsafeFirst.Provider.GetServices<free>());
        _unsafeLastSingle = Catch.Exception(() => _unsafeLast.Provider.GetRequiredService<free>());
        _unsafeLastCollection = Catch.Exception(() => _unsafeLast.Provider.GetServices<free>());
        _safeCollection = _safeOnly.Provider.GetServices<free>().ToArray();
        _closedOverride = _unsafeOpen.Provider.GetRequiredService<generic<int>>();
        _unsafeOpenSingle = Catch.Exception(() => _unsafeOpen.Provider.GetRequiredService<generic<string>>());
        _unsafeOpenCollection = Catch.Exception(() => _unsafeOpen.Provider.GetServices<generic<int>>());
        _unsafeClosedSingle = Catch.Exception(() => _unsafeClosed.Provider.GetRequiredService<generic<int>>());
        _unsafeClosedCollection = Catch.Exception(() => _unsafeClosed.Provider.GetServices<generic<int>>());
        _unrelated = _unsafeClosed.Provider.GetRequiredService<generic<string>>();
    }

    void Destroy()
    {
        foreach (var context in Contexts()) context.Dispose();
    }

    [Fact] void should_allow_the_effective_safe_self_binding() => _safeSingle.ShouldNotBeNull();
    [Fact] void should_reject_the_earlier_unsafe_collection_contributor() => _unsafeFirstCollection.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_invoke_the_earlier_factory() => _unsafeFirst.First.Constructions.ShouldEqual(0);
    [Fact] void should_reject_the_effective_unsafe_factory() => _unsafeLastSingle.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_the_later_unsafe_collection_contributor() => _unsafeLastCollection.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_invoke_the_later_factory() => _unsafeLast.First.Constructions.ShouldEqual(0);
    [Fact] void should_preserve_safe_duplicate_count() => _safeCollection.Length.ShouldEqual(2);
    [Fact] void should_preserve_safe_duplicate_order() => _safeCollection[0].ShouldBeSame(_safeOnly.Provider.GetServices<free>().First());
    [Fact] void should_keep_safe_duplicate_instances_distinct() => Assert.NotSame(_safeCollection[0], _safeCollection[1]);
    [Fact] void should_prefer_the_exact_safe_closed_binding() => _closedOverride.ShouldBeOfExactType<generic<int>>();
    [Fact] void should_reject_the_unsafe_open_single_fallback() => _unsafeOpenSingle.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_include_unsafe_open_contributors_in_closed_collections() => _unsafeOpenCollection.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_construct_from_the_unsafe_open_registration() => _unsafeOpen.First.Constructions.ShouldEqual(0);
    [Fact] void should_reject_an_unsafe_closed_override() => _unsafeClosedSingle.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_reject_an_unsafe_closed_collection_contributor() => _unsafeClosedCollection.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_allow_an_unrelated_safe_closed_construction() => _unrelated.ShouldBeOfExactType<generic<string>>();
    [Fact] void should_not_construct_from_the_unsafe_closed_registration() => _unsafeClosed.First.Constructions.ShouldEqual(0);

    fallback_context[] Contexts() => [_unsafeFirst, _unsafeLast, _safeOnly, _unsafeOpen, _unsafeClosed];
}
