# Commands

Commands represents actions you want to perform against the system.
These are encapsulated as objects that typically is part of the payload for a HTTP Post controller action in the backend.
The command can have validation associated with it and also have business rules associated with it.
In addition to this, the controller can have authorization policies associated with it that applies to the command.

## Proxy Generation

With the [proxy generator](./proxy-generation.md) you'll get the commands generated directly to use in the frontend.
This means you don't have to look at the Swagger API even to know what you have available, the code sits there directly
in the form of a generated proxy object. The generator will look at all HTTP Post actions during compile time and
look for actions marked with `[HttpPost]` and has an argument marked with `[FromBody]` and assume that this is your command
representation / payload.

Take the following controller action in C#:

```csharp
[HttpPost]
public Task Create([FromBody] CreateDebitAccount create) => _eventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));
````

And the command in this case looking like:

```csharp
public record CreateDebitAccount(AccountId AccountId, AccountName Name, PersonId Owner);
```

This all gets generated into the following TypeScript code:

```typescript
import { Command } from '@aksio/frontend/commands';

export class CreateDebitAccount extends Command {
    readonly route: string = '/api/accounts/debit';

    accountId!: string;
    name!: string;
    owner!: string;
}
```

## Usage

To execute the command you simply do the following:

```typescript
const command = new CreateDebitCommand();
command.accountId = 'a23edccc-6cb5-44fd-a7a7-7563716fb080';
command.name = 'My Account';
command.owner = '84cda809-9201-4d8c-8589-0be37c6e3f18';
const result = await command.execute();
```

The result object contains information about whether or not it was successful and any errors that might have occurred, be it
authorization, validation, business rules.
