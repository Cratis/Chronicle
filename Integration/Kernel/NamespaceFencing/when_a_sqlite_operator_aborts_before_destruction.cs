// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_sqlite_operator_aborts_before_destruction.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_sqlite_operator_aborts_before_destruction(context context) : Given<context>(context)
{
    [Fact] void should_acknowledge_safe_replacement() => Context.RequestAccepted.ShouldBeTrue();
    [Fact] void should_open_a_new_lifetime() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.Open));
    [Fact] void should_not_reopen_the_retired_identity() => Context.NewIdentity.ShouldNotEqual(Context.OldIdentity);
    [Fact] void should_not_repeat_creation_notification() => Context.NotificationsAfter.ShouldEqual(Context.NotificationsBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        protected override bool UseSqlite => true;
        public bool RequestAccepted;
        public string StatusAfter = string.Empty;
        public Guid OldIdentity;
        public Guid NewIdentity;
        public ulong NotificationsBefore;
        public ulong NotificationsAfter;

        async Task Because()
        {
            OldIdentity = _oldIdentity;
            NotificationsBefore = await NamespaceNotifications();
            RequestAccepted = (await Abort(_revision)).IsSuccess;
            StatusAfter = (await Status()).Status;
            await CurrentWriter();
            NewIdentity = (await _capability.GetNamespaceMarker(Tenant).Get())!.Incarnation.Value;
            NotificationsAfter = await NamespaceNotifications();
        }
    }
}
