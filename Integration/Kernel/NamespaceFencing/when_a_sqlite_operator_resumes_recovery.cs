// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_a_sqlite_operator_resumes_recovery.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_a_sqlite_operator_resumes_recovery(context context) : Given<context>(context)
{
    [Fact] void should_accept_the_observed_request() => Context.RequestAccepted.ShouldBeTrue();
    [Fact] void should_complete_removal() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.Removed));
    [Fact] void should_retain_the_operation() => Context.OperationAfter.ShouldEqual(Context.OperationBefore);
    [Fact] void should_retain_prior_attempts() => Context.TotalAttempts.ShouldBeGreaterThan(5);
    [Fact] void should_bound_the_new_budget() => Context.BudgetStart.ShouldEqual(5);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        protected override bool UseSqlite => true;
        public bool RequestAccepted;
        public string StatusAfter = string.Empty;
        public Guid OperationBefore;
        public Guid OperationAfter;
        public int TotalAttempts;
        public int BudgetStart;

        async Task Because()
        {
            OperationBefore = _operation;
            RequestAccepted = (await Resume(_revision)).IsSuccess;
            var status = await Status();
            StatusAfter = status.Status;
            OperationAfter = status.OperationId;
            TotalAttempts = status.TotalAttempts;
            BudgetStart = status.BudgetStartAttempts;
        }
    }
}
