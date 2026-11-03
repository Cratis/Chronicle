// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_VerifyContent.when_comparing;

public class and_only_provenance_identifies_the_appended_generation : given.a_generation_history
{
    void Establish()
    {
        _history = _history with { AppendedGeneration = null };
        _stored = _stored with { Context = _stored.Context with { AppendedGeneration = null } };
        ChangeMigrations();
    }

    async Task Because() => _result = await Verify();

    [Fact] void should_use_the_recorded_appended_source() => _result.Result.ShouldEqual(ContentVerificationResult.Equal);
}
