// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Client = global::Cratis.Chronicle;
using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_lookup_observes_recovery_required.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_lookup_observes_recovery_required(context context) : Given<context>(context)
{
    [Fact] void should_return_the_terminal_status() => Context.LookupStatus.ShouldEqual(Client.Namespaces.NamespaceBindingStatus.RecoveryRequired);
    [Fact] void should_not_supply_an_identity() => Context.HasIdentity.ShouldBeFalse();
    [Fact] void should_refuse_writer_enrollment() => Context.EnrollmentError.ShouldBeOfExactType<Client.Namespaces.NamespaceBindingNotValid>();
    [Fact] void should_not_ensure_or_recreate_the_lifecycle() => Context.RevisionAfter.ShouldEqual(Context.RevisionBefore);
    [Fact] void should_not_change_the_marker() => Context.TouchAfter.ShouldEqual(Context.TouchBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public Client.Namespaces.NamespaceBindingStatus LookupStatus;
        public bool HasIdentity;
        public Exception EnrollmentError = null!;
        public long RevisionBefore;
        public long RevisionAfter;
        public long TouchBefore;
        public long TouchAfter;

        async Task Because()
        {
            RevisionBefore = _revision;
            TouchBefore = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
            var binding = await ((Client.Namespaces.INamespaceBindings)_store).LookupNamespaceBinding(Tenant);
            LookupStatus = binding.Status;
            HasIdentity = binding.Incarnation is not null;
            EnrollmentError = Catch.Exception(() => ((Client.Namespaces.INamespaceBoundEventSequences)_store)
                .GetEventSequence(Client.EventSequences.EventSequenceId.Log, binding))!;
            RevisionAfter = (await Status()).Revision;
            TouchAfter = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
        }
    }
}
