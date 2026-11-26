// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.EventSequences.for_CanonicalJson;

/// <summary>
/// A <c language="json">date-time</c> value stored with the host's offset used to be held as a local <see cref="DateTime"/> and is now
/// held as a <see cref="DateTimeOffset"/> with that offset. The canonical text hashes are computed over must not change.
/// </summary>
public class when_encoding_a_date_time_value_stored_with_the_host_offset : Specification
{
    static readonly DateTime _local = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Local).AddTicks(1234567);

    string _heldAsDateTimeOffset;
    string _heldAsLocalDateTime;

    void Because()
    {
        _heldAsDateTimeOffset = CanonicalJson.Encode(Content(new DateTimeOffset(_local)));
        _heldAsLocalDateTime = CanonicalJson.Encode(Content(_local));
    }

    [Fact] void should_encode_the_value_as_it_did_before() => _heldAsDateTimeOffset.ShouldEqual(_heldAsLocalDateTime);

    static ExpandoObject Content(object moment)
    {
        var content = new ExpandoObject();
        ((IDictionary<string, object?>)content)["moment"] = moment;
        return content;
    }
}
