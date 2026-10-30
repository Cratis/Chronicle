// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_the_stamp_has_an_unknown_version : given.a_composite_key_sink
{
    readonly ILogger<Sink> _logger = Substitute.For<ILogger<Sink>>();
    readonly List<string> _warnings = [];
    string _stamp;

    protected override ISqlSinkHarness CreateHarness()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!)).Do(call => _warnings.Add(call[2]!.ToString()!));
        return new SqlSinkHarness { Logger = _logger };
    }

    async Task Because()
    {
        await Write("a", "b", "before");
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE composite_shadows SET __compositeKey='~k9:future' WHERE id='a_b'");
        await Write("a", "b", "after");
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b'").SingleAsync();
    }

    [Fact] void should_never_overwrite_an_unknown_version() => _stamp.ShouldEqual("~k9:future");
    [Fact] void should_not_report_an_unknown_version_as_a_collision() => _warnings.Exists(message => message.Contains("collision", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
