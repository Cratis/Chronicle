// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema.given;

public class a_long_name_schema_store : Specification
{
    protected ILongNameSchemaStore _store;
    protected LongReadModelSchema _schema;
    protected ReadModelTableLocation _long;
    protected ReadModelTableLocation _short;

    void Establish()
    {
        _store = Substitute.For<ILongNameSchemaStore>();
        _schema = new LongReadModelSchema(_store);
        _long = new("chronicle_long_names", "container_hash");
        _short = new(null, "container");
    }
}
