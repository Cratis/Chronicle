// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_every_format_is_encoded : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    List<string> _failures;

    void Because()
    {
        _failures = [];
        var components = new UniqueConstraintComponent[]
        {
            new(Guid.Empty, "guid"),
            new(Guid.Empty, "uuid"),
            new((short)1, "int16"),
            new(1, "int32"),
            new(1L, "int64"),
            new((byte)1, "byte"),
            new(1u, "uint32"),
            new(1ul, "uint64"),
            new(1.5f, "float"),
            new(1.5d, "double"),
            new(1.5m, "decimal"),
            new(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), "date-time"),
            new(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "date-time-offset"),
            new(new DateOnly(2026, 1, 2), "date"),
            new(new TimeOnly(3, 4, 5), "time"),
            new(TimeSpan.FromMinutes(1), "duration")
        };
        foreach (var component in components)
        {
            try
            {
                UniqueConstraintEncoder.Encode(_key, [component]);
            }
            catch (Exception)
            {
                _failures.Add(component.Format!);
            }
        }
    }

    [Fact] void should_encode_every_supported_format() => _failures.ShouldBeEmpty();
}
