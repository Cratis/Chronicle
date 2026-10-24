// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator;

public class when_a_schema_property_collides_with_a_shadow : given.a_read_model_migrator
{
    long _shadows;
    readonly ILogger<ReadModelMigrator> _warnings = Substitute.For<ILogger<ReadModelMigrator>>();
    readonly List<string> _messages = [];
    bool _warned;
    string _refusal;
    string _value;

    void Establish()
    {
        _warnings.IsEnabled(LogLevel.Warning).Returns(true);

        // Capture immediately: source-generated logging returns its mutable state to a pool after Log.
        _warnings.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!))
            .Do(call => _messages.Add(call[2]!.ToString()!));
        _migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), _warnings);
    }

    async Task Because()
    {
        var columns = ProjectedColumns.ForSchema(await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"__ORDER_amount":{"type":"string"}}}"""));
        await using var context = CreateContext("colliding", columns);
        await context.EnsureTableExists();
        await context.Database.ExecuteSqlRawAsync("INSERT INTO colliding(id,amount,__ORDER_amount) VALUES ('row','193.580','user property')");
        var row = await context.Entries.SingleAsync();
        _value = (string)row["__ORDER_amount"]!;
        _shadows = await context.Database.SqlQuery<long>($"SELECT count(*) AS Value FROM pragma_table_xinfo('colliding') WHERE hidden=2").SingleAsync();
        _warned = _messages.Exists(message => message.Contains("__order_amount", StringComparison.Ordinal) && message.Contains("colliding", StringComparison.Ordinal));
        try
        {
            await ShadowColumns.Access(context, "__order_amount", () => Task.FromResult(0));
            _refusal = string.Empty;
        }
        catch (InvalidOperationException exception)
        {
            _refusal = exception.Message;
        }
    }

    [Fact] void should_preserve_the_schema_property() => _value.ShouldEqual("user property");
    [Fact] void should_skip_the_colliding_shadow() => _shadows.ShouldEqual(0);
    [Fact] void should_log_a_warning() => _warned.ShouldBeTrue();
    [Fact] void should_name_the_table_in_the_refusal() => _refusal.ShouldContain("colliding");
    [Fact] void should_name_the_shadow_in_the_refusal() => _refusal.ShouldContain("__order_amount");
}
