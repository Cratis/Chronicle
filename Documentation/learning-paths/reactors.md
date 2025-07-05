# Reactor

Creating a **Reactor** offers the most flexibility. It can be used in any scenario where you want to react to an event being appended.
This means it can perform tasks beyond just producing the current state of your application.
It is ideal for *if-this-then-that* scenarios but can also be used for data creation.

Let's start by defining a read model that will be used in the reducer.

{{snippet:Quickstart-User}}

The following code reacts to the `UserOnboarded` event and then creates a new `User` and inserts into a MongoDB database.

{{snippet:Quickstart-UsersReactor}}

> Note: The code leverages a `Globals` object that is found as part of the full sample and is configured with the
> MongoDB database to use.

The method `Onboarded` is not defined by the `IReactor` interface. The `IReactor` interface serves as a marker interface for discovery purposes.
Methods in a reactor are convention-based, meaning they will be automatically detected and invoked as long as they adhere to the expected signature conventions.

Supported method signatures include:

```csharp
void <MethodName>(EventType @event);
void <MethodName>(EventType @event, EventContext context);
Task <MethodName>(EventType @event);
Task <MethodName>(EventType @event, EventContext context);
```

Opening your database client, you should be able to see the users:

![](./mongodb-users.png)
