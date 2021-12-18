// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Compliance
{
    // /// <summary>
    // /// Extension methods for convenience of working with the <see cref="IComplianceMetadataResolver"/>.
    // /// </summary>
    // public static class ComplianceMetadataResolverExtensions
    // {
    //     /// <summary>
    //     /// Check if there is metadata for a given <see cref="JSchemaTypeGenerationContext"/>.
    //     /// </summary>
    //     /// <param name="resolver"><see cref="IComplianceMetadataResolver"/> to use.</param>
    //     /// <param name="context">The <see cref="JSchemaTypeGenerationContext"/>.</param>
    //     /// <returns>True if there is metadata, false if not.</returns>
    //     public static bool HasMetadata(this IComplianceMetadataResolver resolver, JSchemaTypeGenerationContext context)
    //     {
    //         if (resolver.HasMetadata(context.ObjectType))
    //         {
    //             return true;
    //         }

    //         if (context.ObjectType.DeclaringType == null || context.MemberProperty == null)
    //         {
    //             return false;
    //         }

    //         var property = context.GetProperty();
    //         return resolver.HasMetadata(property);
    //     }

    //     /// <summary>
    //     /// Get metadata for a given <see cref="JSchemaTypeGenerationContext"/>.
    //     /// </summary>
    //     /// <param name="resolver"><see cref="IComplianceMetadataResolver"/> to use.</param>
    //     /// <param name="context">The <see cref="JSchemaTypeGenerationContext"/>.</param>
    //     /// <returns>The <see cref="ComplianceMetadata"/> for the context.</returns>
    //     public static ComplianceMetadata GetMetadataFor(this IComplianceMetadataResolver resolver, JSchemaTypeGenerationContext context)
    //     {
    //         if (resolver.HasMetadata(context.ObjectType))
    //         {
    //             return resolver.GetMetadataFor(context.ObjectType);
    //         }

    //         var property = context.GetProperty();
    //         return resolver.GetMetadataFor(property);
    //     }

    //     static PropertyInfo GetProperty(this JSchemaTypeGenerationContext context) => context.ObjectType.DeclaringType!.GetProperty(context.MemberProperty!.UnderlyingName!, BindingFlags.Public | BindingFlags.Instance)!;
    // }
}
