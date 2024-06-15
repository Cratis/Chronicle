// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600

using System.Net;
using Cratis.Applications.Queries;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.API.Server;

public class QueryResultOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var returnType = context.MethodInfo.ReturnType;
        if (returnType.IsAssignableTo(typeof(Task)) && returnType.IsGenericType)
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        var response = operation.Responses["200"];
        response.Content = response.Content.Where(_ => _.Key == "application/json").ToDictionary(_ => _.Key, _ => _.Value);
        var resultType = typeof(QueryResult<>).MakeGenericType(returnType);
        var schema = context.SchemaGenerator.GenerateSchema(resultType, context.SchemaRepository);
        foreach (var (_, mediaType) in response.Content)
        {
            mediaType.Schema = schema;
        }

        operation.Responses[((int)HttpStatusCode.Conflict).ToString()] = new OpenApiResponse()
        {
            Description = "Conflict",
            Content = response.Content
        };
    }
}
