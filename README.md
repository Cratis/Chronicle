# cratis

## Specifications
### Running continuously

Running specifications continuously shortens the feedback loop by building and running on every change and save.

Simply navigate to the specifications project you're working on and then run the following:

```shell
$ dotnet watch run
````

### Debugging a continuous run

To debug a test run, you will have to rely on attaching the debugger.
The problem is that the test project isn't really running continuously, it is the watcher
that is doing that and spawns a test run for every change.

To be able to get into to the actual test runner process, we leverage the debugging API
and wait for the debugger to hit. Much like one would do with the `debugger` instruction in
JavaScript.

When you're ready to debug, add the following line either close to where you want to debug
or anywhere before what you want to look at:

```csharp
while (!System.Diagnostics.Debugger.IsAttached) Thread.Sleep(10);
```

When you then save, it will start up again and sit and wait on this line.
Then use the configured `.NET Core Attach` launch configuration in VSCode and
select the `dotnet testhost.dll` process.


