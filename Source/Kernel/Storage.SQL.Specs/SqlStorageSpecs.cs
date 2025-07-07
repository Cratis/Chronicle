// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.SQL.Models;
using Cratis.Chronicle.Storage.SQL.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NJsonSchema;
using Xunit;

namespace Cratis.Chronicle.Storage.SQL.Sinks;

public class SqlSinkFactorySpecs
{
    [Fact]
    public void ShouldCreateSqlSinkFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSqlStorage(options =>
        {
            options.ProviderType = SqlProviderType.SqlServer;
            options.ConnectionString = "Server=localhost;Database=test;Integrated Security=true;";
        });
        
        services.AddSingleton<IExpandoObjectConverter, TestExpandoObjectConverter>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var factory = serviceProvider.GetService<SinkFactory>();
        
        // Assert
        Assert.NotNull(factory);
        Assert.Equal(WellKnownSinkTypes.SQL, factory.TypeId);
    }
}

public class SqlSchemaGeneratorSpecs
{
    [Fact]
    public void ShouldGenerateCreateTableSqlForSqlServer()
    {
        // Arrange
        var generator = new SqlSchemaGenerator(SqlProviderType.SqlServer, "dbo");
        var schema = JsonSchema.FromType<TestModel>();
        var model = new Model("test-model", schema);
        
        // Act
        var sql = generator.GenerateCreateTableSql(model);
        
        // Assert
        Assert.Contains("CREATE TABLE", sql);
        Assert.Contains("projection_test_model", sql);
        Assert.Contains("Id NVARCHAR(255) PRIMARY KEY", sql);
    }
    
    [Fact]
    public void ShouldGenerateCreateTableSqlForPostgreSQL()
    {
        // Arrange
        var generator = new SqlSchemaGenerator(SqlProviderType.PostgreSQL, "public");
        var schema = JsonSchema.FromType<TestModel>();
        var model = new Model("test-model", schema);
        
        // Act
        var sql = generator.GenerateCreateTableSql(model);
        
        // Assert
        Assert.Contains("CREATE TABLE IF NOT EXISTS", sql);
        Assert.Contains("projection_test_model", sql);
        Assert.Contains("Id NVARCHAR(255) PRIMARY KEY", sql);
    }
}

public class TestModel
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

public class TestExpandoObjectConverter : IExpandoObjectConverter
{
    public ExpandoObject ToExpandoObject(System.Text.Json.Nodes.JsonObject document, JsonSchema schema)
    {
        return new ExpandoObject();
    }

    public System.Text.Json.Nodes.JsonObject ToJsonObject(ExpandoObject expandoObject, JsonSchema schema)
    {
        return new System.Text.Json.Nodes.JsonObject();
    }
}