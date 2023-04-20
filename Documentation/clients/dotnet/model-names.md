# Model names

When you have read models that gets persisted to a data store the name in which they are identified with in the
target data store is very important. The name typically manifests itself as the name of the MongoDB collection or
the SQL table.

The default convention when configuring the Cratis client is that the name of the type gets pluralized and then
camel-cased. This means if you have a type called `Employee` representing your read model, its collection/table
name will automatically become `employees`.

## Namespaced convention

Out of the box there is a convention that includes the namespace in the model name.
When configuring the `ClientBuilder` you can use it in the following way

```csharp
using Aksio.Cratis.Models;

clientBuilder.UseModelNameConvention(new NamespacedModelNameConvention());
```

The convention supports the following optional parameters on the constructor:

| Parameter | Description | Default |
| --------- | ----------- | ------- |
| segmentsToSkip | Number of segments in the namespace to skip. | 0 |
| separator | Character to use for separating namespace segments. | - |

## Custom convention

You can easily create your own custom convention by implementing the `IModelNameConvention` interface.
The interface has one method that you need to implement:

```csharp
string GetNameFor(Type type);
```

```csharp
using Aksio.Cratis.Models;
using Aksio.Cratis.Strings;
using Humanizer;

public class MyModelNameConvention : IModelNameConvention
{
    public string GetNameFor(Type type) => $"MyCoolService-{type.Name}";
}
```

You hook this up easily using the following:

```csharp
clientBuilder.UseModelNameConvention(new MyModelNameConvention());
```

## Override per model

You can specify the target name of the model directly on the read model by using the `[ModelName]` attribute.
This will then be honored over the convention configured.

```csharp
[ModelName("allEmployees"))]
public record Employee(Guid Id, string FirstName, string LastName);
```
