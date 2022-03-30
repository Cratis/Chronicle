# Command Tracker

If you want to track commands and create an aggregation of the status of commands at a compositional
level, the command tracker provides a React context for this.
This is typically useful when having something like a top level toolbar with a **Save** button that
you want to enable or disable depending on whether or not there are changes within any components
used within it.

Using the toolbar scenario as an example; at the top level we can wrap everything in the `<CommandTracker>`
component. This will establish a React context for this part of the hierarchy and track any commands
used within any descendants.

```typescript
import { CommandTracker } from '@aksio/cratis-applications-frontend/commands';

export const MyComposition = () => {
    return (
        <CommandTracker>
            <Toolbar/>
            <FirstComponent/>
            <SecondComponent/>
        </CommandTracker>
    );
};
```

The command tracker will provide a React context that can be consumed.
Within this context there are the following that can be used:

| Name | Description |
| ---- | ----------- |
| hasChanges | Boolean property that tells whether or not there are changes in any commands within the context |
| execute | Method for executing all commands within the context |
| revertChanges | Method for reverting any changes to commands within the context |

To consume the command tracker context you can use the hook that is provided.

```typescript
import { useCommandTracker } from '@aksio/cratis-applications-frontend/commands';

export const Toolbar = () => {
    const commandTracker = useCommandTracker();

    return (
        <div>
            <button disabled={!commandTracker.hasChanges}>Save</button>
        </div>
    );
};
```

The hook is a convenience hook that makes it easier to get the context.
You can also consume the context directly by using its consumer:

```typescript
import { CommandTrackerContext } from '@aksio/cratis-applications-frontend/commands';

export const Toolbar = () => {
    return (
        <div>
            <CommandTrackerContext.Consumer>
                {value => {
                    return (
                        <button disabled={!value.hasChanges}>Save</button>
                    )
                }}
            </CommandTrackerContext.Consumer>
        </div>
    );
};
```

For the `<FirstComponent>` we could then have something like below:

```typescript
export const FirstComponent = () => {
    const myCommand = MyCommand.use();

    return (
        <div>
            <input type="text" value={command.someValue} onChange={(e,v) => myCommand.someValue = v; }/>
        </div>
    )
}
```

For simplicity `<SecondComponent>` exactly the same, just a different command:

```typescript
export const SecondComponent = () => {
    const myCommand = MyOtherCommand.use();

    return (
        <div>
            <input type="text" value={command.someValue} onChange={(e,v) => myCommand.someValue = v; }/>
        </div>
    )
}
```

Any changes to properties within these commands will bubble up to the context and the state `hasChanges` will be affected
by it.
