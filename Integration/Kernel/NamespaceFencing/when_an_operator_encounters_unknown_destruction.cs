// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_an_operator_encounters_unknown_destruction.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_an_operator_encounters_unknown_destruction(context context) : Given<context>(context)
{
    [Fact] void should_refuse_to_resume_unresolved_work() => Context.ResumeCodes.ShouldEqual<string>([NamespaceErrorCodes.RecoveryRequired]);
    [Fact] void should_refuse_to_abort_unknown_destruction() => Context.AbortCodes.ShouldEqual<string>([NamespaceErrorCodes.RemovalCannotBeAborted]);
    [Fact] void should_preserve_the_in_progress_intent() => Context.AttemptInProgress.ShouldBeTrue();
    [Fact] void should_preserve_the_unknown_destructive_outcome() => Context.DestructiveOutstanding.ShouldBeTrue();
    [Fact] void should_preserve_the_destructive_issue_record() => Context.DestructiveIssued.ShouldBeTrue();
    [Fact] void should_keep_recovery_required() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.RecoveryRequired));
    [Fact] void should_not_advance_the_revision() => Context.RevisionAfter.ShouldEqual(Context.RevisionBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public string[] ResumeCodes = [];
        public string[] AbortCodes = [];
        public bool AttemptInProgress;
        public bool DestructiveOutstanding;
        public bool DestructiveIssued;
        public string StatusAfter = string.Empty;
        public long RevisionBefore;
        public long RevisionAfter;

        async Task Because()
        {
            await SeedUnknownDestructiveIntent();
            RevisionBefore = _revision;
            ResumeCodes = (await Resume(_revision)).ExceptionMessages.ToArray();
            AbortCodes = (await Abort(_revision)).ExceptionMessages.ToArray();
            var status = await Status();
            AttemptInProgress = status.AttemptInProgress;
            DestructiveOutstanding = status.DestructiveAttemptOutstanding;
            DestructiveIssued = status.DestructiveActionIssued;
            StatusAfter = status.Status;
            RevisionAfter = status.Revision;
        }
    }
}
