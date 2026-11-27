// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Services;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalOperators;

public class when_an_authorized_resume_needs_recovery : given.an_operator_pipeline
{
    void Establish()
    {
        _grants.Add(("shop", WellKnownCapabilities.ManageEventStore));
        _incarnations.Resume("shop", "tenant", _operation, Revision)
            .Returns(Task.FromException(new TargetInvocationException(new NamespaceRecoveryRequired("private-tenant"))));
    }

    async Task Because() => _result = await CommandExecutor.Execute(_pipeline, new ResumeNamespaceRemoval("shop", "tenant", _operation, Revision));

    [Fact] void should_authorize_the_manager() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_discover_the_model_bound_handler() => _incarnations.Received(1).Resume("shop", "tenant", _operation, Revision);
    [Fact] void should_discover_the_mapper_and_preserve_its_literal_code() => _result.ExceptionMessages.ShouldEqual<string>(["NamespaceRecoveryRequired"]);
    [Fact] void should_not_leak_the_stack() => _result.ExceptionStackTrace.ShouldBeEmpty();
    [Fact] void should_preserve_a_correlation_identity() => _result.CorrelationId.ShouldNotEqual(Guid.Empty);
}
