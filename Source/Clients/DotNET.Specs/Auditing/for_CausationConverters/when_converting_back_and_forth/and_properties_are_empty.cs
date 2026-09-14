// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Auditing.for_CausationConverters.when_converting_back_and_forth;

public class and_properties_are_empty : Specification
{
    Causation _result;

    void Because() => _result = new Contracts.Auditing.Causation().ToClient();

    [Fact] void should_keep_properties_empty() => _result.Properties.ShouldBeEmpty();
}
