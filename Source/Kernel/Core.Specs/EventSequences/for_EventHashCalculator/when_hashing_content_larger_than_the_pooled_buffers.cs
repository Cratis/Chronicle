// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_hashing_content_larger_than_the_pooled_buffers : Specification
{
    EventHashCalculator _calculator;
    ExpandoObject _content;
    string _identifier;
    string _canonical;
    string _encoded;
    EventHash _largeHash;
    EventHash _emptyHash;
    EventHash _emptyIdentifiersHash;

    void Establish()
    {
        _calculator = new();
        _identifier = new string('ø', 600);
        _content = new ExpandoObject();
        ((IDictionary<string, object?>)_content)[new string('\n', 300)] = new string('x', 8192);
        _canonical = $"{{\"{string.Concat(Enumerable.Repeat("\\n", 300))}\":\"{new string('x', 8192)}\"}}";
    }

    void Because()
    {
        _encoded = CanonicalJson.Encode(_content);
        _largeHash = _calculator.Calculate(_identifier, _identifier, _content);
        _emptyHash = _calculator.Calculate("type", "source", new ExpandoObject());
        _emptyIdentifiersHash = _calculator.Calculate(new EventTypeId(string.Empty), new EventSourceId(string.Empty), new ExpandoObject());
    }

    [Fact] void should_preserve_long_escaped_names_and_values() => _encoded.ShouldEqual(_canonical);
    [Fact] void should_preserve_long_utf8_identifiers() => _largeHash.Value.ShouldEqual(Hash($"{_identifier}|{_identifier}|{_canonical}"));
    [Fact] void should_not_hash_previous_buffer_contents() => _emptyHash.Value.ShouldEqual(Hash("type|source|{}"));
    [Fact] void should_preserve_empty_identifiers() => _emptyIdentifiersHash.Value.ShouldEqual(Hash("||{}"));

    static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
