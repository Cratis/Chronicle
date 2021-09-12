// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Cratis.Reflection;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.GraphQL
{
    public class SchemaRoute : ObjectType
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        readonly List<SchemaRoute> _children = new();
        readonly List<SchemaRouteItem> _items = new();

        public SchemaRoute(string path, string localName, string typeName)
        {
            Path = path;
            LocalName = localName;
            TypeName = typeName;
        }

        public string Path { get; }
        public string LocalName { get; }
        public string TypeName { get; }
        public bool HasItems => _items.Count > 0;

        public IEnumerable<SchemaRoute> Children => _children;
        public IEnumerable<SchemaRouteItem> Items => _items;

        public void AddChild(SchemaRoute child)
        {
            _children.Add(child);
        }

        public void AddItem(SchemaRouteItem item)
        {
            _items.Add(item);
        }

        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name(TypeName);

            foreach (var item in _items)
            {
                var fieldDescriptor = descriptor
                    .Field(item.Method)
                    .Name(item.Name)
                    .Resolve((ctx) => InvokeResolver(ctx, item));

                AddAdornedAuthorization(item, fieldDescriptor);
            }

            foreach (var child in _children)
            {
                descriptor.Field(child.LocalName).Type(child).Resolver(_ => new object());
            }

            if (_items.Count == 0)
            {
                descriptor.Field("Default").Resolve(() => "Configure your first item");
            }
        }

        object InvokeResolver(IResolverContext context, SchemaRouteItem item)
        {
            var arguments = new List<object>();

            foreach (var parameter in item.Method.GetParameters())
            {
                Expression<Func<NameString, object>> expression = (NameString name) => context.ArgumentValue<object>(name);
                var genericArgumentMethod = expression.GetMethodInfo().GetGenericMethodDefinition();
                var argumentMethod = genericArgumentMethod.MakeGenericMethod(parameter.ParameterType);
                arguments.Add(argumentMethod.Invoke(context, new object[] { (NameString)parameter.Name! })!);
            }

            var service = ServiceProvider!.GetService(item.Method.DeclaringType!);
            var result = item.Method.Invoke(service, arguments.ToArray());
            if (result != null && item.Method.IsAsync())
            {
                var awaiter = result.GetType()!.GetMethod(nameof(Task<object>.GetAwaiter))!.Invoke(result, Array.Empty<object>())!;
                return awaiter.GetType().GetMethod(nameof(TaskAwaiter<object>.GetResult))!.Invoke(awaiter, Array.Empty<object>())!;
            }

            return result!;
        }

        void AddAdornedAuthorization(SchemaRouteItem item, IObjectFieldDescriptor fieldDescriptor)
        {
            var authorizeAttributes = new List<AuthorizeAttribute>();
            authorizeAttributes.AddRange((AuthorizeAttribute[])item.Method.GetCustomAttributes(typeof(AuthorizeAttribute), true)!);
            authorizeAttributes.AddRange((AuthorizeAttribute[])item.Method.DeclaringType!.GetCustomAttributes(typeof(AuthorizeAttribute), true)!);

            foreach (var authorizeAttribute in authorizeAttributes)
            {
                if (string.IsNullOrEmpty(authorizeAttribute.Policy))
                {
                    fieldDescriptor.Authorize();
                }
                else
                {
                    fieldDescriptor.Authorize(authorizeAttribute.Policy);
                }
            }
        }
    }
}
