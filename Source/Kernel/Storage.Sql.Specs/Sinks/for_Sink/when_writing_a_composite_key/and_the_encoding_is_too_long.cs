// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_the_encoding_is_too_long : given.a_composite_key_sink
{
    readonly ILogger<Sink> _logger = Substitute.For<ILogger<Sink>>();
    readonly List<string> _warnings = [];
    string? _stamp;
    string _name;

    protected override ISqlSinkHarness CreateHarness()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!))
            .Do(call => _warnings.Add(call[2]!.ToString()!));
        return new SqlSinkHarness { Logger = _logger };
    }

    async Task Because()
    {
        await Write(new string('a', 850), "b", "still written");
        await using var context = CreateContext();
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows").SingleOrDefaultAsync();
        _name = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM composite_shadows").SingleAsync();
    }

    [Fact] void should_leave_the_shadow_unstamped() => _stamp.ShouldBeNull();
    [Fact] void should_preserve_the_write() => _name.ShouldEqual("still written");
    [Fact] void should_warn_about_the_limit() => _warnings.Exists(message => message.Contains("850", StringComparison.Ordinal)).ShouldBeTrue();
}
