// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using Grpc.Net.Client;
using GrpcClients;
using ProtoBuf.Grpc.Configuration;

Console.WriteLine("\nGrpc Clients Generator\n");

if (args.Length != 2)
{
    Console.WriteLine("Usage: GrpcClients <grpc contracts assembly> <output assembly name>");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var outputAssemblyName = args[1];
var outputAssemblyPath = $"{Path.GetDirectoryName(assemblyPath)}/{outputAssemblyName}.dll";
var assembly = Assembly.LoadFrom(assemblyPath);
var services = assembly.ExportedTypes.Where(_ => _.IsInterface).ToArray();

var assemblyBuilder = new PersistedAssemblyBuilder(new(outputAssemblyName), typeof(void).Assembly);
var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

var channel = GrpcChannel.ForAddress("https://localhost:5001");
var callInvoker = channel.CreateCallInvoker();

var clientFactory = ClientFactory.Create();
var createClient = clientFactory.GetType().GetMethods().SingleOrDefault(_ => _.Name == nameof(ClientFactory.CreateClient) && _.ContainsGenericParameters)!;

var contractCount = 0;

foreach (var serviceType in services)
{
    var service = createClient.MakeGenericMethod(serviceType).Invoke(clientFactory, [callInvoker])!;
    var implementationType = (service.GetType() as TypeInfo)!;

    var typeBuilder = moduleBuilder.DefineType($"{serviceType.Namespace}.{serviceType.Name.Substring(1)}", TypeAttributes.Public | TypeAttributes.Class, implementationType.BaseType);
    typeBuilder.AddInterfaceImplementation(serviceType);

    List<FieldBuilder> fields = [];

    foreach (var field in implementationType.DeclaredFields)
    {
        fields.Add(typeBuilder.DefineField(field.Name, field.FieldType, field.Attributes));
    }

    var module = implementationType.Assembly.Modules.First();
    foreach (var constructor in implementationType.DeclaredConstructors)
    {
        var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, constructor.GetParameters().Select(_ => _.ParameterType).ToArray());
        var ilGenerator = constructorBuilder.GetILGenerator();
        var ilBytes = constructor.GetMethodBody()!.GetILAsByteArray() ?? [];
        var reader = new MethodBodyReader(module, fields, ilBytes);
        var code = reader.GetBodyCode();
        reader.Write(ilGenerator);
    }

    foreach (var method in implementationType.DeclaredMethods)
    {
        var methodNameSegments = method.Name.Split('.');
        var methodName = methodNameSegments[^1];
        var serviceMethod = serviceType.GetMethod(methodName)!;
        var attributes = method.Attributes;
        if (serviceMethod is not null)
        {
            attributes = MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual;
        }

        var methodBuilder = typeBuilder.DefineMethod(method.Name, attributes, method.CallingConvention, method.ReturnType, method.GetParameters().Select(_ => _.ParameterType).ToArray());
        var ilGenerator = methodBuilder.GetILGenerator();
        var ilBytes = method.GetMethodBody()!.GetILAsByteArray() ?? [];

        if (method.Attributes.HasFlag(MethodAttributes.Virtual))
        {
            var baseMethod = serviceMethod ?? implementationType.BaseType!.GetMethod(methodName)!;
            if (baseMethod is not null)
            {
                typeBuilder.DefineMethodOverride(methodBuilder, baseMethod);
            }
        }
        var reader = new MethodBodyReader(module, fields, ilBytes);
        reader.Write(ilGenerator);

        contractCount++;
    }

    typeBuilder.CreateType();
}

Console.WriteLine("Generated {0} service implementations", contractCount);

assemblyBuilder.Save(outputAssemblyPath);
