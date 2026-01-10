// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBConverter"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoDBConverter"/> class.
/// </remarks>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between <see cref="ExpandoObject"/> to <see cref="BsonDocument"/>.</param>
/// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
/// <param name="readModel"><see cref="ReadModelDefinition"/> the converter is for.</param>
public class MongoDBConverter(
    IExpandoObjectConverter expandoObjectConverter,
    ITypeFormats typeFormats,
    ReadModelDefinition readModel) : IMongoDBConverter
{
    /// <inheritdoc/>
    public MongoDBProperty ToMongoDBProperty(PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        var arrayFilters = new ArrayFilters();
        var propertyBuilder = new StringBuilder();
        var currentPropertyPath = new PropertyPath(string.Empty);

        foreach (var segment in propertyPath.Segments)
        {
            if (propertyBuilder.Length > 0)
            {
                propertyBuilder.Append('.');
            }

            currentPropertyPath += segment;
            switch (segment)
            {
                case PropertyName:
                    {
                        var propertyName = segment.Value.ToMongoDBPropertyName();
                        propertyBuilder.Append(propertyName);
                    }
                    break;

                case ArrayProperty:
                    {
                        var collectionIdentifier = currentPropertyPath.LastSegment.Value.ToCamelCase();
                        if (arrayIndexers.HasFor(currentPropertyPath))
                        {
                            var arrayIndexer = arrayIndexers.GetFor(currentPropertyPath);
                            var arrayPropertyName = segment.Value.ToMongoDBPropertyName();
                            propertyBuilder.AppendFormat("{0}.$[{1}]", arrayPropertyName, collectionIdentifier);
                            var filter = new ExpandoObject();
                            var key = arrayIndexer.IdentifierProperty.Path.ToMongoDBPropertyName();
                            ((IDictionary<string, object?>)filter).Add($"{collectionIdentifier}.{key}", arrayIndexer.Identifier);
                            var document = ToBsonValue(filter) as BsonDocument;
                            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(document));
                        }
                        else
                        {
                            var arrayPropertyName = segment.Value.ToMongoDBPropertyName();
                            propertyBuilder.Append(arrayPropertyName);
                        }
                    }
                    break;
            }
        }

        var property = propertyBuilder.ToString();
        return new(property, arrayFilters);
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(Key key)
    {
        var schema = readModel.GetSchemaForLatestGeneration();
        var idPropertyName = schema.HasKeyProperty() ? schema.GetKeyProperty().Name : schema.GetLikelyKeyPropertyName();

        var bsonValue = key.Value is ExpandoObject ?
                expandoObjectConverter.ToBsonDocument((key.Value as ExpandoObject)!, schema.GetSchemaForPropertyPath(idPropertyName)) :
                ToBsonValue(key.Value, idPropertyName);

        // If the schema does not have the Id property, we assume it is the event source identifier, which is of type string.
        return bsonValue == BsonNull.Value ? new BsonString(key.Value.ToString()) : bsonValue;
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(object input)
    {
        if (input is null) return BsonNull.Value;

        if (input is ExpandoObject expandoObject)
        {
            var expandoObjectAsDictionary = expandoObject as IDictionary<string, object>;
            var document = new BsonDocument();

            foreach (var kvp in expandoObjectAsDictionary)
            {
                document[kvp.Key.ToMongoDBPropertyName()] = ToBsonValue(kvp.Value);
            }

            return document;
        }

        var bsonValue = input.ToBsonValue();
        if (bsonValue == BsonNull.Value && input is IEnumerable enumerable)
        {
            var array = new BsonArray();

            foreach (var item in enumerable)
            {
                array.Add(ToBsonValue(item));
            }

            return array;
        }

        return bsonValue;
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(object? input, PropertyPath property)
    {
        var schemaProperty = readModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(property);
        return schemaProperty is not null ? ToBsonValue(input, schemaProperty) : ToBsonValue(input!);
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(object? input, JsonSchemaProperty schemaProperty)
    {
        if (input is null) return BsonNull.Value;

        if (typeFormats.IsKnown(schemaProperty.Format!))
        {
            var targetType = typeFormats.GetTypeForFormat(schemaProperty.Format!);
            return input.ToBsonValue(targetType);
        }

        if (input is ExpandoObject expandoObject)
        {
            return expandoObjectConverter.ToBsonDocument(expandoObject, schemaProperty.ActualTypeSchema);
        }

        var bsonValue = input.ToBsonValue();
        if (bsonValue != BsonNull.Value)
        {
            return bsonValue;
        }

        if (input is IEnumerable enumerable)
        {
            var items = new List<BsonValue>();
            foreach (var item in enumerable)
            {
                if (item is ExpandoObject itemAsExpandoObject)
                {
                    items.Add(expandoObjectConverter.ToBsonDocument(itemAsExpandoObject, schemaProperty.Item!));
                }
                else
                {
                    items.Add(ToBsonValue(item, schemaProperty));
                }
            }
            return new BsonArray(items);
        }

        var value = input.ToBsonValueBasedOnSchemaPropertyType(schemaProperty);
        return value == BsonNull.Value ? BsonNull.Value : value;
    }
}
