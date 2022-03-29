# Command Tracker

If you have a component that acts as a composition of other components and you want to provide
something like a common toolbar with a **Save** button to perform all changes of different commands
in one go.

```typescript
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


```typescript
export const Toolbar = () => {
    const commandTracker = useCommandTracker();

    return (
        <div>
            <button disabled={!commandTracker.hasChanges}>Save</button>
        </div>
    );
};
```

Alternatively

```typescript
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

```typescript
export const FirstComponent = () => {
    const myCommand = MyCommand.use();

    return (
        <div>
            <input type="text" onChange={(e,v) => myCommand.someValue = v; }/>
        </div>
    )
}
```

```typescript
export const SecondComponent = () => {
    const myCommand = MyOtherCommand.use();

    return (
        <div>
            <input type="text" onChange={(e,v) => myCommand.someValue = v; }/>
        </div>
    )
}
```
