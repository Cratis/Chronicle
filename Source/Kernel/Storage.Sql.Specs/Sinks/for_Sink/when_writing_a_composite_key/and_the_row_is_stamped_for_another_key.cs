// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_the_row_is_stamped_for_another_key : given.a_composite_key_sink
{
    readonly ILogger<Sink> _logger = Substitute.For<ILogger<Sink>>();
    readonly List<string> _warnings = [];
    string _stamp;
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
        await Write("a_b", "c", "owner");
        await Write("a", "b_c", "merged as before");
        await using var context = CreateContext();
        _stamp = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows WHERE id='a_b_c'").SingleAsync();
        _name = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM composite_shadows WHERE id='a_b_c'").SingleAsync();
    }

    [Fact] void should_keep_the_first_authoritative_stamp() => _stamp.ShouldEqual("~k1:3:a_b1:c");
    [Fact] void should_preserve_the_existing_write_merge() => _name.ShouldEqual("merged as before");
    [Fact] void should_warn_about_the_legacy_key_collision() => _warnings.Exists(message => message.Contains("collision", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
}
