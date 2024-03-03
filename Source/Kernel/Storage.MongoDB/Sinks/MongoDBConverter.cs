// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Text;
using Cratis.Kernel.Keys;
using Cratis.Models;
using Cratis.Properties;
using Cratis.Schemas;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Driver;
using NJsonSchema;

namespace Cratis.Kernel.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBConverter"/>.
/// </summary>
public class MongoDBConverter : IMongoDBConverter
{
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ITypeFormats _typeFormats;
    readonly Model _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBConverter"/> class.
    /// </summary>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between <see cref="ExpandoObject"/> to <see cref="BsonDocument"/>.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for looking up actual types.</param>
    /// <param name="model"><see cref="Model"/> the converter is for.</param>
    public MongoDBConverter(
        IExpandoObjectConverter expandoObjectConverter,
        ITypeFormats typeFormats,
        Model model)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _typeFormats = typeFormats;
        _model = model;
    }

    /// <inheritdoc/>
    public MongoDBProperty ToMongoDBProperty(PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        var arrayFilters = new List<BsonDocumentArrayFilterDefinition<BsonDocument>>();
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
                        propertyBuilder.Append(segment.Value);
                    }
                    break;

                case ArrayProperty:
                    {
                        var collectionIdentifier = currentPropertyPath.LastSegment.Value.ToCamelCase();
                        if (arrayIndexers.HasFor(currentPropertyPath))
                        {
                            var arrayIndexer = arrayIndexers.GetFor(currentPropertyPath);
                            propertyBuilder.AppendFormat("{0}.$[{1}]", segment.Value, collectionIdentifier);
                            var filter = new ExpandoObject();
                            ((IDictionary<string, object?>)filter).Add($"{collectionIdentifier}.{arrayIndexer.IdentifierProperty}", arrayIndexer.Identifier);
                            var document = ToBsonValue(filter) as BsonDocument;
                            arrayFilters.Add(new BsonDocumentArrayFilterDefinition<BsonDocument>(document));
                        }
                        else
                        {
                            propertyBuilder.Append(segment.Value);
                        }
                    }
                    break;
            }
        }

        var property = propertyBuilder.ToString();
        property = GetNameForPropertyInBsonDocument(property);
        return new(property, arrayFilters.ToArray());
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(Key key)
    {
        var bsonValue = key.Value is ExpandoObject ?
                _expandoObjectConverter.ToBsonDocument((key.Value as ExpandoObject)!, _model.Schema.GetSchemaForPropertyPath("id")) :
                ToBsonValue(key.Value, "id");

        // If the schema does not have the Id property, we assume it is the event source identifier, which is of type string.
        if (bsonValue == BsonNull.Value)
        {
            return new BsonString(key.Value.ToString());
        }

        return bsonValue;
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
                document[GetNameForPropertyInBsonDocument(kvp.Key)] = ToBsonValue(kvp.Value);
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
        BsonValue value = BsonNull.Value;
        var schemaProperty = _model.Schema.GetSchemaPropertyForPropertyPath(property);
        if (schemaProperty is not null)
        {
            return ToBsonValue(input, schemaProperty);
        }

        return value;
    }

    /// <inheritdoc/>
    public BsonValue ToBsonValue(object? input, JsonSchemaProperty schemaProperty)
    {
        if (input is null) return BsonNull.Value;

        if (_typeFormats.IsKnown(schemaProperty.Format))
        {
            var targetType = _typeFormats.GetTypeForFormat(schemaProperty.Format);
            return input.ToBsonValue(targetType);
        }

        if (input is ExpandoObject expandoObject)
        {
            return _expandoObjectConverter.ToBsonDocument(expandoObject, schemaProperty.ActualTypeSchema);
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
                    items.Add(_expandoObjectConverter.ToBsonDocument(itemAsExpandoObject, schemaProperty.Item));
                }
                else
                {
                    items.Add(ToBsonValue(item, schemaProperty));
                }
            }
            return new BsonArray(items);
        }

        var value = input.ToBsonValueBasedOnSchemaPropertyType(schemaProperty);
        if (value == BsonNull.Value)
        {
            return BsonNull.Value;
        }

        return value;
    }

    string GetNameForPropertyInBsonDocument(string name)
    {
        if (name == "id")
        {
            return "_id";
        }
        return name;
    }
}
