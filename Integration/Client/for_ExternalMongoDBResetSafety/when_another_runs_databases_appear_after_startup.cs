// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_another_runs_databases_appear_after_startup : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify("t_A_", ["billing"],
        ["billing", "t_A_chronicle+main", "t_A_System+es", "t_B_System+es", "t_B_Testing+es", "chr_other_idx_Testing+es+default", "reset_other_testing+es+Default"]));

    [Fact] void should_allow_the_reset() => _error.ShouldBeNull();
}
