// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ResourceNames;

public class when_checking_reserved_characters : Specification
{
    const string Alphabet = "ab+#~$.:@ -_\u0000\u001F\u007F\u0080\u009F\u00A0\u00D8e\u0301";

    int _mismatches;
    int _checked;

    void Because()
    {
        var random = new Random(8);
        for (var i = 0; i < 5000; i++)
        {
            var chars = new char[random.Next(0, 12)];
            for (var c = 0; c < chars.Length; c++)
            {
                chars[c] = Alphabet[random.Next(Alphabet.Length)];
            }

            var name = new string(chars);
            var expected = !name.Any(_ => "+#~$".Contains(_) || char.IsControl(_));
            if (ResourceNames.HasNoReservedCharacters(name) != expected)
            {
                _mismatches++;
            }

            _checked++;
        }
    }

    [Fact] void should_have_checked_names() => _checked.ShouldEqual(5000);
    [Fact] void should_agree_with_the_character_rule() => _mismatches.ShouldEqual(0);
}
