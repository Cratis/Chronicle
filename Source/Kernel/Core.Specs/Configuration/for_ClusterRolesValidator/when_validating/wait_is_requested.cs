// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration.for_ClusterRolesValidator.when_validating;

public class wait_is_requested : Specification
{
    ValidateOptionsResult _result;

    void Because() => _result = new ClusterRolesValidator().Validate(null, new() { Clustering = new() { JobsFallback = JobsFallback.Wait } });

    [Fact] void should_validate_the_configuration() => _result.Failed.ShouldBeTrue();
}
