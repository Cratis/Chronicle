// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.SQL.for_DynamicModelManager.given;

public class a_dynamic_model_manager : Specification
{
    protected DynamicModelManager manager;
    protected SqlStorageOptions storageOptions;
    protected SinkDbContext context;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<SinkDbContext>()
            .UseInMemoryDatabase(databaseName: "test-db")
            .Options;

        context = new SinkDbContext(options);
        
        storageOptions = new SqlStorageOptions
        {
            ProviderType = SqlProviderType.SqlServer,
            Schema = "dbo"
        };

        manager = new DynamicModelManager(context, storageOptions);
    }
}