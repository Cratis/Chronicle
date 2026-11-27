// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_an_operator_targets_the_default_namespace.context;
using NativeNamespace = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_an_operator_targets_the_default_namespace(context context) : Given<context>(context)
{
    [Fact] void should_reject_the_control() => Context.WasRefused.ShouldBeTrue();
    [Fact] void should_not_initialize_or_change_default_control_state() => Context.DefaultRevisionAfter.ShouldEqual(Context.DefaultRevisionBefore);
    [Fact] void should_not_mutate_the_named_recovery_operation() => Context.NamedRevisionAfter.ShouldEqual(Context.NamedRevisionBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public bool WasRefused;
        public long? DefaultRevisionBefore;
        public long? DefaultRevisionAfter;
        public long NamedRevisionBefore;
        public long NamedRevisionAfter;

        async Task Because()
        {
            DefaultRevisionBefore = (await _capability.NamespaceLifecycles.Get(NativeNamespace.Default))?.Revision;
            NamedRevisionBefore = _revision;
            var result = await _services.Services.Namespaces.ResumeNamespaceRemoval(new()
            {
                EventStore = _storeName,
                Namespace = NativeNamespace.Default.Value,
                OperationId = _operation,
                ExpectedRevision = _revision
            });
            WasRefused = !result.IsSuccess && result.HasExceptions;
            DefaultRevisionAfter = (await _capability.NamespaceLifecycles.Get(NativeNamespace.Default))?.Revision;
            NamedRevisionAfter = (await Status()).Revision;
        }
    }
}
