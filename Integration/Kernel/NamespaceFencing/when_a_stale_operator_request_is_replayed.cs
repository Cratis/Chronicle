// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_stale_operator_request_is_replayed.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_stale_operator_request_is_replayed(context context) : Given<context>(context)
{
    [Fact] void should_report_a_conditional_conflict() => Context.Codes.ShouldEqual<string>([NamespaceErrorCodes.WriteConflict]);
    [Fact] void should_keep_recovery_required() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.RecoveryRequired));
    [Fact] void should_not_advance_the_control_revision() => Context.RevisionAfter.ShouldEqual(Context.RevisionBefore);
    [Fact] void should_not_issue_any_marker_effect() => Context.TouchAfter.ShouldEqual(Context.TouchBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public string[] Codes = [];
        public string StatusAfter = string.Empty;
        public long RevisionBefore;
        public long RevisionAfter;
        public long TouchBefore;
        public long TouchAfter;

        async Task Because()
        {
            RevisionBefore = _revision;
            TouchBefore = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
            var result = await Resume(_revision - 1);
            result.IsSuccess.ShouldBeFalse();
            Codes = result.ExceptionMessages.ToArray();
            var status = await Status();
            StatusAfter = status.Status;
            RevisionAfter = status.Revision;
            TouchAfter = (await _capability.GetNamespaceMarker(Tenant).Get())!.Touch;
        }
    }
}
