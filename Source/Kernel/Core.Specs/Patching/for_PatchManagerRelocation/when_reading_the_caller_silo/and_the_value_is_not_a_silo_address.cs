// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patching.for_PatchManagerRelocation.when_reading_the_caller_silo;

public class and_the_value_is_not_a_silo_address : Specification
{
    SiloAddress? _result;
    void Establish() => RequestContext.Set(PatchManagerRelocation.CallerSiloKey, "not-a-silo-address");
    void Because() => _result = PatchManagerRelocation.CallerSilo();
    void Destroy() => RequestContext.Remove(PatchManagerRelocation.CallerSiloKey);
    [Fact] void should_not_name_a_silo() => _result.ShouldBeNull();
}
