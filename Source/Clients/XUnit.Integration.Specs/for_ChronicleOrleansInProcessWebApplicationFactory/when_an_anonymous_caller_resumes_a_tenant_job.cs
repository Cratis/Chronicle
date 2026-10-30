// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleOrleansInProcessWebApplicationFactory;

public class when_an_anonymous_caller_resumes_a_tenant_job : given.a_generated_jobs_service
{
    async Task Because() => await Resume("some-event-store");

    [Fact] void should_use_the_real_arc_pipeline() => ShouldUseTheRealArcPipeline();
    [Fact] void should_use_the_aspnet_policy_runtime() => ShouldUseTheAspNetPolicyRuntime();
    [Fact] void should_keep_authentication_enabled() => ShouldKeepAuthenticationEnabled();
    [Fact] void should_authorize_the_command() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_have_no_validation_failures() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_have_no_execution_exceptions() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_validate_the_job_against_the_grain() => await _jobsManager.Received().GetAllJobs();
    [Fact] async Task should_resume_the_requested_job_once() => await _jobsManager.Received(1).Resume(_jobId);
}
