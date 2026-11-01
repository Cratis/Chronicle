// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNames.when_deriving_a_candidate;

public class and_the_prefix_ends_inside_a_multibyte_character : Specification
{
    [Theory]
    [InlineData("æ")]
    [InlineData("😀")]
    void should_clip_at_a_unicode_boundary(string character)
    {
        var result = ReadModelTableNames.Candidate(DatabaseType.PostgreSql, new string('x', 45) + character + new string('y', 40));
        result[..46].ShouldEqual(new string('x', 45) + "~");
        (Encoding.UTF8.GetByteCount(result) <= 63).ShouldBeTrue();
        Encoding.UTF8.GetString(new UTF8Encoding(false, true).GetBytes(result)).ShouldEqual(result);
    }
}
