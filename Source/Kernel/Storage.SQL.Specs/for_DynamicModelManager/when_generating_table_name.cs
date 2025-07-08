// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.SQL.for_DynamicModelManager;

public class when_generating_table_name : Specification
{
    DynamicModelManager manager;
    SqlStorageOptions storageOptions;

    void Establish()
    {
        storageOptions = new SqlStorageOptions();
        manager = new DynamicModelManager(new TestDbContext(), storageOptions);
    }

    [Fact] void should_generate_correct_name_for_user_profile() 
    {
        var tableName = manager.GetTableName("user-profile");
        tableName.ShouldEqual("projection_user_profile");
    }

    [Fact] void should_generate_correct_name_for_order_items() 
    {
        var tableName = manager.GetTableName("order.items");
        tableName.ShouldEqual("projection_order_items");
    }

    [Fact] void should_generate_correct_name_for_complex_name_example() 
    {
        var tableName = manager.GetTableName("complex-name.example");
        tableName.ShouldEqual("projection_complex_name_example");
    }

    class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("test");
        }
    }
}