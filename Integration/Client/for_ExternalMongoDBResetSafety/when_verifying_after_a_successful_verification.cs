// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_verifying_after_a_successful_verification : Cratis.Specifications.Specification
{
    ExternalMongoDBResetSafety _safety;
    IEnumerable<string> _databaseNames;
    Exception _error;

    void Establish()
    {
        _safety = new();
        _safety.Verify("run_", [], ["run_chronicle+main"]);
        _databaseNames = Substitute.For<IEnumerable<string>>();
    }

    void Because() => _error = Catch.Exception(() => _safety.Verify("run_", [], _databaseNames));

    [Fact] void should_allow_the_reset() => _error.ShouldBeNull();
    [Fact] void should_not_recheck_the_database_names() => _databaseNames.DidNotReceive().GetEnumerator();
    [Fact] void should_remain_verified() => _safety.IsVerified.ShouldBeTrue();
}
