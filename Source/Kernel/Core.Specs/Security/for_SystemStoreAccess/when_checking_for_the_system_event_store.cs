// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security.for_SystemStoreAccess;

public class when_checking_for_the_system_event_store : Specification
{
    public static TheoryData<string> Spellings() => new() { "System", "system", "SYSTEM", "sYsTeM", "\u017Fystem" };

    public static TheoryData<string> OtherNames() => new() { "System ", "Systems", string.Empty };

    [Theory]
    [MemberData(nameof(Spellings))]
    public void should_agree_with_the_storage_comparer(string spelling) =>
        SystemStoreAccess.IsSystem(spelling).ShouldEqual(CaseInsensitiveEventStoreNameComparer.Instance.Equals(new EventStoreName(spelling), EventStoreName.System));

    [Theory]
    [InlineData("System")]
    [InlineData("system")]
    [InlineData("SYSTEM")]
    [InlineData("sYsTeM")]
    public void should_recognize_the_casings_the_storage_layer_resolves(string spelling) => SystemStoreAccess.IsSystem(spelling).ShouldBeTrue();

    [Theory]
    [MemberData(nameof(OtherNames))]
    public void should_not_recognize_other_names(string name) => SystemStoreAccess.IsSystem(name).ShouldBeFalse();

    [Fact] void should_not_recognize_a_missing_name() => SystemStoreAccess.IsSystem(null).ShouldBeFalse();
}
