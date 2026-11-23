// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

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
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for logging.</param>
public class MongoDBConverter(
    IExpandoObjectConverter expandoObjectConverter,
    ITypeFormats typeFormats,
    ReadModelDefinition readModel,
    ILogger<MongoDBConverter> logger) : IMongoDBConverter
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
                        var arrayProperty = ToArrayProperty(currentPropertyPath, arrayIndexers);
                        propertyBuilder.Append(arrayProperty.Property);
                        arrayFilters.AddRange(arrayProperty.ArrayFilters);
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
        if (key.Value is null)
        {
            throw new ReadModelKeyHasNoValue(readModel.Identifier);
        }

        var schema = readModel.GetSchemaForLatestGeneration();
        var idPropertyName = schema.HasKeyProperty() ? schema.GetKeyProperty().Name : schema.GetLikelyKeyPropertyName();

        if (string.IsNullOrWhiteSpace(idPropertyName))
        {
            return new BsonString(key.Value.ToString() ?? string.Empty);
        }

        var bsonValue = key.Value is ExpandoObject ?
            expandoObjectConverter.ToBsonDocument((key.Value as ExpandoObject)!, schema.GetSchemaForPropertyPath(idPropertyName)) :
            ToBsonValue(key.Value, idPropertyName);

        // If the schema does not have the Id property, we assume it is the event source identifier, which is of type string.
        return bsonValue == BsonNull.Value ? new BsonString(key.Value.ToString() ?? string.Empty) : bsonValue;
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(object input)
    {
        if (input is null) return BsonNull.Value;

        if (input is ExpandoObject expandoObject)
        {
            return ToBsonDocument(expandoObject);
        }

        var bsonValue = input.ToBsonValue();
        if (bsonValue == BsonNull.Value && input is IEnumerable enumerable)
        {
            return ToBsonArray(enumerable);
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
            try
            {
                return input.ToBsonValue(targetType);
            }
            catch (Exception)
            {
                // The schema's declared format (e.g. "guid") does not always match the actual value - a
                // ConceptAs<string> key resolved through a schema that reports "guid" is the common shape
                // (see #3844), and the conversion raises a FormatException. Falling back to the untyped
                // BSON representation keeps the value queryable under its real shape instead of freezing
                // the partition with "Unrecognized Guid format."
                logger.KeyNotConvertible(readModel.Identifier, schemaProperty.Name);
                return input.ToBsonValue();
            }
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
            return ToBsonArray(enumerable, schemaProperty);
        }

        var value = input.ToBsonValueBasedOnSchemaPropertyType(schemaProperty);
        return value == BsonNull.Value ? BsonNull.Value : value;
    }

    BsonDocument ToBsonDocument(ExpandoObject input)
    {
        var document = new BsonDocument();
        foreach (var kvp in input)
        {
            document[kvp.Key.ToMongoDBPropertyName()] = ToBsonValue(kvp.Value!);
        }
        return document;
    }

    BsonArray ToBsonArray(IEnumerable input)
    {
        var array = new BsonArray();
        foreach (var item in input)
        {
            array.Add(ToBsonValue(item));
        }
        return array;
    }

    BsonArray ToBsonArray(IEnumerable input, JsonSchemaProperty schemaProperty)
    {
        var items = new List<BsonValue>();
        foreach (var item in input)
        {
            items.Add(item is ExpandoObject expando
                ? expandoObjectConverter.ToBsonDocument(expando, schemaProperty.Item!)
                : ToBsonValue(item, schemaProperty));
        }
        return new BsonArray(items);
    }

    MongoDBProperty ToArrayProperty(PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        var indexers = arrayIndexers.All.Where(_ => _.ArrayProperty == propertyPath).ToArray();
        var propertyName = propertyPath.LastSegment.Value.ToMongoDBPropertyName();
        if (indexers.Length == 0)
        {
            return new(propertyName, []);
        }

        var collectionIdentifier = propertyPath.LastSegment.Value.ToCamelCase();
        var segments = new List<string>();
        var filters = new ArrayFilters();
        for (var level = 0; level < indexers.Length; level++)
        {
            var indexer = indexers[level];
            var identifier = indexers.Length == 1 ? collectionIdentifier : $"{collectionIdentifier}{level}";
            segments.Add($"{propertyName}.$[{identifier}]");
            var identifierPropertyName = indexer.IdentifierProperty.ToMongoDB();
            var identifierPath = propertyPath + indexer.IdentifierProperty;
            var identifierValue = ToBsonValueForArrayIndexer(indexer.Identifier, identifierPath);
            filters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument($"{identifier}.{identifierPropertyName}", identifierValue)));
        }

        return new(string.Join('.', segments), filters);
    }

    BsonValue ToBsonValueForArrayIndexer(object? input, PropertyPath property)
    {
        var schemaProperty = readModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(property);
        return schemaProperty is null ? ToBsonValue(input!) : ToBsonValue(input, schemaProperty);
    }
}
