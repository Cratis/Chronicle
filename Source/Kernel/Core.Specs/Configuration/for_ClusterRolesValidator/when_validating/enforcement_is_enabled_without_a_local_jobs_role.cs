// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ClusterRolesValidator.when_validating;

public class enforcement_is_enabled_without_a_local_jobs_role : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new ClusterRolesValidator().Validate(null, new() { Clustering = new() { EnforceJobsRole = true, Roles = new() { Jobs = false } } });

    [Fact] void should_allow_a_non_worker_silo() => _result.Succeeded.ShouldBeTrue();
}
