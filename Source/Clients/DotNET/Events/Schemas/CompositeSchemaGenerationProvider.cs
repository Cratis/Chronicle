// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;
using Cratis.Concepts;
using Newtonsoft.Json;

namespace Cratis.Events.Schemas
{
    // public class CompositeSchemaGenerationProvider : JSchemaGenerationProvider
    // {
    //     readonly IComplianceMetadataResolver _resolver;

    //     public CompositeSchemaGenerationProvider(IComplianceMetadataResolver resolver)
    //     {
    //         _resolver = resolver;
    //     }

    //     public override JSchema GetSchema(JSchemaTypeGenerationContext context)
    //     {
    //         if (context.ObjectType.IsConcept())
    //         {
    //             var conceptType = context.ObjectType.GetConceptValueType();
    //             if (_resolver.HasMetadata(context))
    //             {
    //                 var metadata = _resolver.GetMetadataFor(context);
    //             }
    //             var schema = context.GetFormatSchemaFor(conceptType);
    //             if (schema == default)
    //             {
    //                 var generator = new JSchemaGenerator();
    //                 schema = generator.Generate(conceptType, context.Required != Required.Always);
    //             }
    //             return schema;
    //         }
    //         else
    //         {
    //             if (_resolver.HasMetadata(context))
    //             {
    //                 var metadata = _resolver.GetMetadataFor(context);
    //             }
    //             var schema = context.GetFormatSchemaFor(context.ObjectType);
    //             if (schema != default)
    //             {

    //             }
    //         }

    //         return null!;
    //     }
    // }
}
