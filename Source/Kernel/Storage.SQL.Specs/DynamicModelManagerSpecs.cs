// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage.SQL.Models;
using Microsoft.EntityFrameworkCore;
using NJsonSchema;
using Xunit;

namespace Cratis.Chronicle.Storage.SQL.Models;

public class DynamicModelManagerSpecs
{
    [Fact]
    public void ShouldCreateTableManagerWithOptions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ProjectionDbContext>()
            .UseInMemoryDatabase(databaseName: "test-db")
            .Options;

        using var context = new ProjectionDbContext(options);
        
        var storageOptions = new SqlStorageOptions
        {
            ProviderType = SqlProviderType.SqlServer,
            Schema = "dbo"
        };

        // Act
        var manager = new DynamicModelManager(context, storageOptions);
        
        // Assert
        Assert.NotNull(manager);
        
        // Verify table name generation
        var tableName = manager.GetTableName("test-projection");
        Assert.Equal("projection_test_projection", tableName);
    }

    [Fact]
    public void ShouldGenerateCorrectTableName()
    {
        // Arrange
        var storageOptions = new SqlStorageOptions();
        var manager = new DynamicModelManager(new TestDbContext(), storageOptions);
        
        // Act & Assert
        Assert.Equal("projection_user_profile", manager.GetTableName("user-profile"));
        Assert.Equal("projection_order_items", manager.GetTableName("order.items"));
        Assert.Equal("projection_complex_name_example", manager.GetTableName("complex-name.example"));
    }

    class TestProjection
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }
    }
}