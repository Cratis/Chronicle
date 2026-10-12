// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyParts;

public class when_formatting_under_a_noncanonical_culture : Specification
{
    CultureInfo _original;
    string _signed64;
    string _unsigned64;
    string _signed32;

    void Establish()
    {
        _original = CultureInfo.CurrentCulture;
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "negative";
        CultureInfo.CurrentCulture = culture;
    }

    void Because()
    {
        _signed64 = SequencingKeyParts.Canonical(-123L);
        _unsigned64 = SequencingKeyParts.Canonical(123UL);
        _signed32 = SequencingKeyParts.Canonical(-123);
    }

    void Destroy() => CultureInfo.CurrentCulture = _original;

    [Fact] void should_use_invariant_signed64_text() => _signed64.ShouldEqual("-123");
    [Fact] void should_use_invariant_unsigned64_text() => _unsigned64.ShouldEqual("123");
    [Fact] void should_use_invariant_signed32_text() => _signed32.ShouldEqual("-123");
}
