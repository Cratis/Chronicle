// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_an_operator_resumes_acknowledged_recovery.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_an_operator_resumes_acknowledged_recovery(context context) : Given<context>(context)
{
    [Fact] void should_accept_the_exact_observed_operation() => Context.RequestAccepted.ShouldBeTrue();
    [Fact] void should_complete_the_real_removal() => Context.StatusAfter.ShouldEqual(nameof(NamespaceLifecycleStatus.Removed));
    [Fact] void should_retain_the_operation_identity() => Context.OperationAfter.ShouldEqual(Context.OperationBefore);
    [Fact] void should_retain_the_attempt_history() => Context.TotalAttempts.ShouldBeGreaterThan(5);
    [Fact] void should_record_the_new_budget_boundary() => Context.BudgetStartAttempts.ShouldEqual(5);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public bool RequestAccepted;
        public string StatusAfter = string.Empty;
        public Guid OperationBefore;
        public Guid OperationAfter;
        public int TotalAttempts;
        public int BudgetStartAttempts;

        async Task Because()
        {
            OperationBefore = _operation;
            var result = await Resume(_revision);
            RequestAccepted = result.IsSuccess;
            var status = await Status();
            StatusAfter = status.Status;
            OperationAfter = status.OperationId;
            TotalAttempts = status.TotalAttempts;
            BudgetStartAttempts = status.BudgetStartAttempts;
        }
    }
}
