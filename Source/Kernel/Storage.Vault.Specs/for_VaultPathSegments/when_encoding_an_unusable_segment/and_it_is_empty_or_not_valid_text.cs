// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultPathSegments.when_encoding_an_unusable_segment;

public class and_it_is_empty_or_not_valid_text : given.segments
{
    Exception? _empty;
    Exception? _highSurrogate;
    Exception? _lowSurrogate;
    Exception? _overlong;
    Exception? _overlongEscaped;
    Exception? _atTheLimit;
    Exception? _atTheLimitEscaped;

    void Because()
    {
        _empty = Refusal(string.Empty);
        _highSurrogate = Refusal("a\uD800");
        _lowSurrogate = Refusal("\uDC00");
        _overlong = Refusal(new string('x', 1025));
        _overlongEscaped = Refusal(new string('\u00D8', 513));
        _atTheLimit = Refusal(new string('x', 1024));
        _atTheLimitEscaped = Refusal(new string('\u00D8', 512));
    }

    [Fact] void should_refuse_an_empty_segment() => _empty.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_a_lone_high_surrogate() => _highSurrogate.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_a_lone_low_surrogate() => _lowSurrogate.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_refuse_more_than_the_limit() => _overlong.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_count_utf8_bytes_against_the_limit() => _overlongEscaped.ShouldBeOfExactType<InvalidEncryptionKeyAddress>();
    [Fact] void should_accept_exactly_the_limit() => _atTheLimit.ShouldBeNull();
    [Fact] void should_accept_utf8_bytes_at_the_limit() => _atTheLimitEscaped.ShouldBeNull();
}
