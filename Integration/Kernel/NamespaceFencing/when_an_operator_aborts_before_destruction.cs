// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_an_operator_aborts_before_destruction.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_an_operator_aborts_before_destruction(context context) : Given<context>(context)
{
    [Fact] void should_acknowledge_the_safe_abort() => Context.RequestAccepted.ShouldBeTrue();
    [Fact] void should_open_the_replacement() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.Open));
    [Fact] void should_never_reopen_the_retired_identity() => Context.NewIdentity.ShouldNotEqual(Context.OldIdentity);
    [Fact] void should_not_repeat_the_creation_notification() => Context.NotificationsAfter.ShouldEqual(Context.NotificationsBefore);
    [Fact] void should_keep_the_old_writer_fenced() => Context.OldWriterWasRefused.ShouldBeTrue();

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public bool RequestAccepted;
        public string StatusAfter = string.Empty;
        public Guid OldIdentity;
        public Guid NewIdentity;
        public ulong NotificationsBefore;
        public ulong NotificationsAfter;
        public bool OldWriterWasRefused;

        async Task Because()
        {
            OldIdentity = _oldIdentity;
            NotificationsBefore = await NamespaceNotifications();
            RequestAccepted = (await Abort(_revision)).IsSuccess;
            StatusAfter = (await Status()).Status;
            await CurrentWriter();
            NewIdentity = (await _capability.GetNamespaceMarker(Tenant).Get())!.Incarnation.Value;
            NotificationsAfter = await NamespaceNotifications();
            var error = await Catch.Exception(() => _oldWriter.Append("source", new NamespaceFenceValue("retired")));
            OldWriterWasRefused = IsFenceRefusal(error!);
        }
    }
}
