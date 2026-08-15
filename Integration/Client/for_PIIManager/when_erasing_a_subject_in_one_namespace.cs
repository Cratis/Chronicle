// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Integration.for_PIIManager.when_erasing_a_subject_in_one_namespace.context;

namespace Cratis.Chronicle.Integration.for_PIIManager;

/// <summary>
/// An erasure reaches every event store in its namespace, because that is exactly how far a subscription can carry
/// a subject's key. It stops there, and this pins the stopping.
/// </summary>
/// <remarks>
/// The namespace is the tenancy boundary, and the same identifier in two namespaces is two people as far as
/// Chronicle is concerned. Widening the erasure to cover every event store would be a defect rather than a fix if
/// it also crossed that line: one tenant's right-to-erasure request would blank another tenant's data.
/// </remarks>
/// <param name="context">The context the facts assert against.</param>
[Collection(ChronicleCollection.Name)]
public class when_erasing_a_subject_in_one_namespace(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public const string EventStoreName = "pii-namespace-scoping";
        public const string OtherNamespace = "pii-namespace-scoping-other";

        /// <summary>
        /// Gets the event source the subject's event is appended to. Per run, so this spec and its siblings
        /// cannot see each other's keys through the kernel collection they share.
        /// </summary>
        public EventSourceId EventSourceId { get; } = $"request-{Guid.NewGuid():N}";
        public Subject Subject { get; } = $"person-{Guid.NewGuid():N}";
        public string SocialSecurityNumber { get; } = "111-22-3333";

        public bool ErasedNamespaceHasKeyBeforeErasure { get; private set; }
        public bool OtherNamespaceHasKeyBeforeErasure { get; private set; }
        public IEnumerable<EventStoreNamespaceName> EnumeratedNamespaces { get; private set; } = [];

        public bool ErasedNamespaceHasKeyAfterErasure { get; private set; } = true;
        public bool OtherNamespaceHasKeyAfterErasure { get; private set; }
        public bool OtherNamespaceIsFencedAfterErasure { get; private set; } = true;
        public string PiiInErasedNamespaceAfterErasure { get; private set; } = string.Empty;
        public string PiiInOtherNamespaceAfterErasure { get; private set; } = string.Empty;

        public override IEnumerable<Type> EventTypes => [typeof(PersonRegistered)];

        async Task Because()
        {
            var keys = Services.GetRequiredService<IEncryptionKeyStorage>();
            var erasedNamespace = await ChronicleClient.GetEventStore(EventStoreName);
            var otherNamespace = await ChronicleClient.GetEventStore(EventStoreName, OtherNamespace);

            await Task.WhenAll(erasedNamespace.DiscoverAll(), otherNamespace.DiscoverAll());
            await Task.WhenAll(erasedNamespace.EventTypes.Register(), otherNamespace.EventTypes.Register());

            var registration = new PersonRegistered(Subject, "Jane Doe", SocialSecurityNumber);
            await erasedNamespace.EventLog.Append(EventSourceId, registration);
            await otherNamespace.EventLog.Append(EventSourceId, registration);

            ErasedNamespaceHasKeyBeforeErasure = await keys.HasFor(EventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            OtherNamespaceHasKeyBeforeErasure = await keys.HasFor(EventStoreName, OtherNamespace, Subject.Value);
            EnumeratedNamespaces = await erasedNamespace.GetNamespaces();

            await erasedNamespace.PII.DeleteEncryptionKeyFor(Subject.Value);

            ErasedNamespaceHasKeyAfterErasure = await keys.HasFor(EventStoreName, Concepts.EventStoreNamespaceName.Default, Subject.Value);
            OtherNamespaceHasKeyAfterErasure = await keys.HasFor(EventStoreName, OtherNamespace, Subject.Value);
            OtherNamespaceIsFencedAfterErasure = await keys.GetErasureFor(EventStoreName, OtherNamespace, Subject.Value) is not null;
            PiiInErasedNamespaceAfterErasure = await ReadSocialSecurityNumber(erasedNamespace);
            PiiInOtherNamespaceAfterErasure = await ReadSocialSecurityNumber(otherNamespace);
        }

        static async Task<string> ReadSocialSecurityNumber(IEventStore eventStore)
        {
            var events = await eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            return ((PersonRegistered)events.First(_ => _.Context.SequenceNumber == EventSequenceNumber.First).Content).SocialSecurityNumber;
        }
    }

    [Fact]
    void should_hold_the_key_in_the_erased_namespace_before_erasure() =>
        Context.ErasedNamespaceHasKeyBeforeErasure.ShouldBeTrue();

    [Fact]
    void should_hold_a_separate_key_in_the_other_namespace_before_erasure() =>
        Context.OtherNamespaceHasKeyBeforeErasure.ShouldBeTrue();

    [Fact]
    void should_enumerate_the_other_namespace() =>
        Context.EnumeratedNamespaces.ShouldContain(new EventStoreNamespaceName(context.OtherNamespace));

    [Fact]
    void should_remove_the_key_from_the_erased_namespace() =>
        Context.ErasedNamespaceHasKeyAfterErasure.ShouldBeFalse();

    [Fact]
    void should_leave_the_other_namespace_holding_its_key() =>
        Context.OtherNamespaceHasKeyAfterErasure.ShouldBeTrue();

    [Fact]
    void should_not_fence_the_other_namespace() =>
        Context.OtherNamespaceIsFencedAfterErasure.ShouldBeFalse();

    [Fact]
    void should_blank_the_pii_in_the_erased_namespace() =>
        Context.PiiInErasedNamespaceAfterErasure.ShouldEqual(string.Empty);

    [Fact]
    void should_keep_the_pii_readable_in_the_other_namespace() =>
        Context.PiiInOtherNamespaceAfterErasure.ShouldEqual(Context.SocialSecurityNumber);
}
