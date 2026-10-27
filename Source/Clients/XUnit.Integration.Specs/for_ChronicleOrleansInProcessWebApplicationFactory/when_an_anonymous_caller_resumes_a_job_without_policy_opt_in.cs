// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

public class when_an_anonymous_caller_resumes_a_job_without_policy_opt_in : given.a_generated_jobs_service
{
    async Task Because() => await Resume("some-event-store", allowAnonymousPolicy: false);

    [Fact] void should_use_the_real_arc_pipeline() => ShouldUseTheRealArcPipeline();
    [Fact] void should_use_the_aspnet_policy_runtime() => ShouldUseTheAspNetPolicyRuntime();
    [Fact] void should_keep_authentication_enabled() => ShouldKeepAuthenticationEnabled();
    [Fact] void should_deny_the_command() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_report_the_denial() => _result.AuthorizationFailureReason.ShouldNotBeEmpty();
    [Fact] void should_not_report_an_execution_exception() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_not_resume_the_job() => await _jobsManager.DidNotReceive().Resume(_jobId);
}
