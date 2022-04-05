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
public Task OpenDebitAccount([FromBody] OpenDebitAccount create) => _eventLog.Append(create.AccountId, new DebitAccountOpened(create.Name, create.Owner));
````

And the command in this case looking like:

```csharp
public record OpenDebitAccount(AccountId AccountId, AccountName Name, PersonId Owner);
```

This all gets generated into a TypeScript representation:

```typescript
import { Command, CommandValidator, CommandPropertyValidators, useCommand, SetCommandValues } from '@aksio/cratis-applications-frontend/commands';
import { Validator } from '@aksio/cratis-applications-frontend/validation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit');

export interface IOpenDebitAccount {
    accountId?: string;
    name?: string;
    owner?: string;
}

export class OpenDebitAccountValidator extends CommandValidator {
    readonly properties: CommandPropertyValidators = {
        accountId: new Validator(),
        name: new Validator(),
        owner: new Validator(),
    };
}

export class OpenDebitAccount extends Command implements IOpenDebitAccount {
    readonly route: string = '/api/accounts/debit';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly validation: CommandValidator = new OpenDebitAccountValidator();

    private _accountId!: string;
    private _name!: string;
    private _owner!: string;

    get requestArguments(): string[] {
        return [
        ];
    }

    get properties(): string[] {
        return [
            'accountId',
            'name',
            'owner',
        ];
    }

    get accountId(): string {
        return this._accountId;
    }

    set accountId(value: string) {
        this._accountId = value;
        this.propertyChanged('accountId');
    }
    get name(): string {
        return this._name;
    }

    set name(value: string) {
        this._name = value;
        this.propertyChanged('name');
    }
    get owner(): string {
        return this._owner;
    }

    set owner(value: string) {
        this._owner = value;
        this.propertyChanged('owner');
    }

    static use(initialValues?: IOpenDebitAccount): [OpenDebitAccount, SetCommandValues<IOpenDebitAccount>] {
        return useCommand<OpenDebitAccount, IOpenDebitAccount>(OpenDebitAccount, initialValues);
    }
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

## Data binding and initial values

The command holds properties that is the payload of what you want to have happen.
These properties are subject to validation rules and also business rules.
They are also often sourced from a read model coming from a [query](./queries.md).
Meaning that you are performing operations that in many cases are in practice updates
to existing data.

Instead of binding your frontend components to the read models from the queries, you
can bind to the properties on the command. The benefit of this is that you will then
have any validation rules that is running on the frontend automatically run as values
change. In addition, the command itself will be tracking whether or not there are changes
from the original data. On the command you'll find a property called `hasChanges` that
will return `true` if it has changes and `false` if not.

To be able to do this, the command needs to be given a set of initial values that it will
use to compare current state against.

```typescript
const command = new CreateDebitCommand();
command.setInitialValues({
    accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080';
    name: 'My Account';
    owner: '84cda809-9201-4d8c-8589-0be37c6e3f18';
});
```

At this stage `hasChanges` will be returning `false`.
If we alter something like name:

```typescript
command.name = 'My other account';
```

`hasChanges` will now be `true`.

If you have a component that has sub components that all work with different commands,
there is a way to track the state of `hasChanges` for all these. Read more about the [command tracker](./command-tracker.md) for this.

## React

Working with the raw command can be less than intuitive in a React context which has a different
approach to lifecycle. From the generated TypeScript proxy you'll notice there is also a React hook added to the type.
The purpose of this hook is to join the React rendering pipeline and provide re-rendering for state
that is useful, such as knowing whether or not there are changes.

```typescript
export const MyComponent = () => {
    const [openDebitAccount] = OpenDebitAccount.use();

    return (
        <></>
    )
};
```

The hook can also take initial values:

```typescript
export const MyComponent = () => {
    const [openDebitAccount] = OpenDebitAccount.use({
        accountId: 'a23edccc-6cb5-44fd-a7a7-7563716fb080';
        name: 'My Account';
        owner: '84cda809-9201-4d8c-8589-0be37c6e3f18';
    });

    return (
        <></>
    )
};
```

In addition, the hook returns a value in the tuple that enables setting values on the command directly.
You can modify the properties directly as well, since it is being tracked as a React state. This is more
for convenience and giving more of a **React** consistent programming model.
