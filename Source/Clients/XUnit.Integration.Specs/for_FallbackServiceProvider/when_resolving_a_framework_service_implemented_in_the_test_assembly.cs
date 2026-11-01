// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_FallbackServiceProvider;

public class when_resolving_a_framework_service_implemented_in_the_test_assembly : Specification
{
    readonly fallback_context _typeBinding = new();
    readonly fallback_context _instance = new();
    readonly fallback_context _framework = new();
    readonly fallback_context _nonShared = new();
    readonly fallback_context _dependency = new();
    Exception? _typeBindingError;
    Exception? _instanceError;
    Exception? _dependencyError;
    ICloneable _frameworkResult;
    ICloneable _nonSharedResult;

    void Establish()
    {
        Build(_typeBinding, ServiceDescriptor.Transient(typeof(ICloneable), typeof(cloneable)));
        Build(_instance, ServiceDescriptor.Singleton<ICloneable>(new cloneable(new())));
        Build(_framework, ServiceDescriptor.Transient(typeof(ICloneable), typeof(ArrayList)));
        Build(_nonShared, ServiceDescriptor.Transient(typeof(ICloneable), typeof(cloneable)), shared: false);
        Build(_dependency, ServiceDescriptor.Transient(typeof(ICloneable), typeof(cloneable)), dependency: true);
        _typeBinding.Advance();
        _instance.Advance();
        _framework.Advance();
        _nonShared.Advance();
        _dependency.Advance();
    }

    void Because()
    {
        _typeBindingError = Catch.Exception(() => _typeBinding.Provider.GetRequiredService<ICloneable>());
        _instanceError = Catch.Exception(() => _instance.Provider.GetRequiredService<ICloneable>());
        _frameworkResult = _framework.Provider.GetRequiredService<ICloneable>();
        _nonSharedResult = _nonShared.Provider.GetRequiredService<ICloneable>();
        _dependencyError = Catch.Exception(() => _dependency.Provider.GetRequiredService<IComparable>());
    }

    void Destroy()
    {
        _typeBinding.Dispose();
        _instance.Dispose();
        _framework.Dispose();
        _nonShared.Dispose();
        _dependency.Dispose();
    }

    [Fact] void should_reject_a_test_implementation_type() => _typeBindingError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_rejected_service() => Assert.Contains(typeof(ICloneable).FullName!, _typeBindingError?.Message ?? string.Empty);
    [Fact] void should_explain_the_current_context_registration() => Assert.Contains("ConfigureServices", _typeBindingError?.Message ?? string.Empty);
    [Fact] void should_not_construct_the_first_context_implementation() => _typeBinding.First.Constructions.ShouldEqual(0);
    [Fact] void should_reject_a_test_implementation_instance() => _instanceError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_build_a_framework_implementation() => _frameworkResult.ShouldBeOfExactType<ArrayList>();
    [Fact] void should_keep_the_fallback_for_a_non_shared_host() => _nonSharedResult.ShouldBeOfExactType<cloneable>();
    [Fact] void should_reject_when_the_built_container_resolves_it_as_a_dependency() => _dependencyError.ShouldBeOfExactType<InvalidOperationException>();

    static void Build(fallback_context context, ServiceDescriptor descriptor, bool shared = true, bool dependency = false)
    {
        ServiceDescriptor[] captured = [descriptor];
        context.Build((services, _) =>
        {
            context.Registry.Update(captured);
            services.AddContextServiceForwarders(captured, shared ? typeof(fallback_context).Assembly : null);
            if (dependency)
            {
                services.AddTransient<IComparable>(sp => new comparer(sp.GetRequiredService<ICloneable>()));
            }
        }, shared);
    }

    sealed class cloneable : ICloneable
    {
        public cloneable(fallback_context.tracker tracker) => tracker.Constructions++;

        public object Clone() => this;
    }

    sealed class comparer(ICloneable dependency) : IComparable
    {
        public int CompareTo(object? obj) => ReferenceEquals(dependency, obj) ? 0 : 1;
    }
}
