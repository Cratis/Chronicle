// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Namespaces;

using context = Cratis.Chronicle.Kernel.Integration.NamespaceFencing.when_an_operator_targets_another_removal_operation.context;

namespace Cratis.Chronicle.Kernel.Integration.NamespaceFencing;

[Collection(NamespaceFenceCollection.Name)]
public class when_an_operator_targets_another_removal_operation(context context) : Given<context>(context)
{
    [Fact] void should_report_the_precondition_conflict() => Context.Codes.ShouldEqual<string>([NamespaceErrorCodes.WriteConflict]);
    [Fact] void should_not_replace_the_real_operation() => Context.OperationAfter.ShouldEqual(Context.OperationBefore);
    [Fact] void should_not_advance_the_revision() => Context.RevisionAfter.ShouldEqual(Context.RevisionBefore);

    public class context(NamespaceFenceFixture fixture) : given.a_recoverable_removal(fixture)
    {
        public string[] Codes = [];
        public Guid OperationBefore;
        public Guid OperationAfter;
        public long RevisionBefore;
        public long RevisionAfter;

        async Task Because()
        {
            OperationBefore = _operation;
            RevisionBefore = _revision;
            var result = await _services.Services.Namespaces.AbortNamespaceRemoval(new()
            {
                EventStore = _storeName,
                Namespace = Tenant,
                OperationId = Guid.NewGuid(),
                ExpectedRevision = _revision
            });
            Codes = result.ExceptionMessages.ToArray();
            var status = await Status();
            OperationAfter = status.OperationId;
            RevisionAfter = status.Revision;
        }
    }
}
