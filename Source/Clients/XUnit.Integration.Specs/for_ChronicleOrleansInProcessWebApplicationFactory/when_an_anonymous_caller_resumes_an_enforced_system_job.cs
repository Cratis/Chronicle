// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

public class when_an_anonymous_caller_resumes_an_enforced_system_job : given.a_generated_jobs_service
{
    async Task Because() => await Resume("System", enforceSystem: true);

    [Fact] void should_use_the_real_arc_pipeline() => ShouldUseTheRealArcPipeline();
    [Fact] void should_use_the_aspnet_policy_runtime() => ShouldUseTheAspNetPolicyRuntime();
    [Fact] void should_keep_authentication_enabled() => ShouldKeepAuthenticationEnabled();
    [Fact] void should_enforce_system_operations() => _systemEnforced.ShouldBeTrue();

    // Arc 22.143.0 reports policy denials without an AuthorizationFailureReason; assert the reason once Arc supplies one.
    [Fact] void should_deny_the_command() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_report_validation_failures() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_not_report_an_execution_exception() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(_jobId);
}
