// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.SQL.for_DynamicModelManager;

public class when_creating_table_manager_with_options : given.a_dynamic_model_manager
{
    [Fact] void should_create_manager() => manager.ShouldNotBeNull();
    
    [Fact] void should_generate_correct_table_name_for_simple_projection() 
    {
        var tableName = manager.GetTableName("test-projection");
        tableName.ShouldEqual("projection_test_projection");
    }
}