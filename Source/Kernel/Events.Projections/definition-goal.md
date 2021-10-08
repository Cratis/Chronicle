# Definition Goal

## JSON

Known types:

| Name | Description |
| ---- | ----------- |
| $event | The custom that holds the content |
| $occurred | The date/time when the event occurred |
| $eventSourceId | The unique identifier for the event source instance |
| $model | The target model |

```json
{
    "identifier": "565c2247-0ce4-4f25-8c5d-5afe1ca89113",
    "projectTo": {
        "modelName": "something",
        "resource": "memory | mongodb | postgresql | elastic"
    },
    "from": {
        "25d74c03-2542-4d52-bb53-4dce6ada24c6": {
            "someProperty": "$event.someProperty",
            "formattedProperty": "{{$event.firstName}} {{$event.lastName}}",
            "incrementedProperty": "$increment",
            "addedProperty": "$add($event.someValue)"
        },
        "f998e862-cac0-4ccc-86a1-cad0269db30e": {
            "key": "$event.id",    // Defaults to using the EventSourceId as the key, if not specified
            "$model.someOtherProperty": "$event.someOtherProperty"
        }
    },
    "removedWith": {
        "cd9e4ab9-7f67-460b-a5f7-5830d4bb4716": {
            "key": "$event.id"
        }
    },
    "join": {
        "52bd8b37-4811-4edc-9f6b-07e2d965c110": {
            "$on": "$model.relationProperty",
            "$key": "$event.id",
            "$properties": {
                "$model.thirdProperty": "$event.propertyFromTheThirdEvent"
            }
        }
    },
    "children": {
        "model.children": {
            "key": "$childModel.id",
            "from": {
                "3cf2b919-9102-4d05-86bf-94c39abc1224": {
                    "key": "$event.id",
                    "$model.property": "$event.property"
                }
            },
            "removedWith": {
                "1bcbc402-6930-4fbc-8e28-b86ecc0db2e8": {
                    "key": "$event.id"
                }
            }
        }
    }
}
```

## CSharp

```csharp
[Projection("610bbd9c-4024-40db-8bd2-38ea1481904d)]
public class MyProjection : IProjection
{
    public void Define(IProjectionBuilder builder)
    {
        builder
            .ProjectTo<MyModel>("<optional name>")
            .From<SomeEvent>(_ => _
                .Set(model => model.SomeProperty).To(@event => @event.SomeProperty))
            .From<SomeOtherEvent>(_ => _
                .Set(model => model.SomeOtherProperty).To(@event => @event.SomeOtherProperty))
            .RemovedWith<SomeDeleteEvent>(_ => _
                .UsingKey(@event => @event.Id)) // Default to using the EventSourceId as the key, if not specified
            .Join<ThirdEvent>(_ => _
                .On(model => model.RelationProperty)
                .UsingKey(@event => @event.Id) // Default to using the EventSourceId as the key, if not specified
                .Set(model => model.ThirdProperty).To(@event => @event.PropertyFromTheThirdEvent))
            .Children<SomeChildModel>(_ => _
                .StoredIn(model => model.Children)
                .IdentifiedBy(childModel => childModel.Id))
                .From<ChildAdded>(cb => cb
                    .UsingKey(@event => @event.Id) // Default to using the EventSourceId as the key, if not specified
                    cb.Set(childMOdel => childModel.Property).To(@event => @event.Property))
                .RemovedWith<ChildRemoved>(cb => cb
                    .UsingKey(@event => @event.Id)) // Default to using the EventSourceId as the key, if not specified

    }
}
```
