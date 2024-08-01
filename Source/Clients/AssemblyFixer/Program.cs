using Mono.Cecil;

var assemblyPath = "/Volumes/Code/Cratis/Chronicle/Source/Clients/Orleans/bin/release/net8.0/Cratis.Chronicle.Orleans.dll";

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
var customAttributes = assembly.CustomAttributes.Where(_ => _.AttributeType.FullName == "Orleans.ApplicationPartAttribute");
var attributesToRemove = customAttributes.Where(attribute =>
{
    var argument = attribute.ConstructorArguments[0].Value.ToString() ?? string.Empty;
    return attribute.ConstructorArguments.Count == 1 && argument.StartsWith("Cratis.Chronicle") && argument != "Cratis.Chronicle.Orleans";
}).ToList();

attributesToRemove.ForEach(attribute =>
{
    Console.WriteLine($"Removing assembly part attribute for '{attribute.ConstructorArguments[0].Value}'");
    assembly.CustomAttributes.Remove(attribute);
});

assembly.Write(Path.GetFileName(assemblyPath));
