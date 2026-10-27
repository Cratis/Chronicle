// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline;

/// <summary>
/// Compact accessor keys still detect mutability and accessibility changes.
/// </summary>
public class when_an_accessor_changes : given.a_contract_difference
{
    Exception? _mutabilityError;
    Exception? _visibilityError;

    void Because()
    {
        var setter = _baseline.Replace("::Names init public", "::Names set public", StringComparison.Ordinal);
        var visibility = _baseline.Replace("::Names init public", "::Names init protected", StringComparison.Ordinal);
        _mutabilityError = Catch.Exception(() => given.public_api_difference.RequireMatch(_baseline, setter));
        _visibilityError = Catch.Exception(() => given.public_api_difference.RequireMatch(_baseline, visibility));
    }

    [Fact] void should_reject_mutability_changes() => _mutabilityError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_report_the_new_setter() => _mutabilityError!.Message.ShouldContain("::Names set public");
    [Fact] void should_reject_visibility_changes() => _visibilityError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_report_the_new_visibility() => _visibilityError!.Message.ShouldContain("::Names init protected");
}
