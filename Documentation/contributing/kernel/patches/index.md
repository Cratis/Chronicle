# Version Patch System

Chronicle includes a version-based patch system to handle breaking changes during upgrades. Patches are automatically discovered and applied during server startup, ensuring the system state is migrated correctly between versions.

## Overview

The patch system:

- **Automatically discovers** patches using reflection (`IInstancesOf<ICanApplyPatch>`)
- **Applies patches in order** based on semantic version
- **Tracks applied patches** to prevent re-execution
- **Supports rollback** through `Down()` methods for safe downgrades
- **Runs at startup** before other Chronicle services initialize

## Key Concepts

### SemanticVersion

Patches are tied to semantic versions (e.g., `1.5.0`, `2.0.0-beta.1`). The system compares the current system version with patch versions to determine which patches to apply.

### Patch Lifecycle

1. **Discovery**: On startup, all `ICanApplyPatch` implementations are discovered
2. **Filtering**: Only patches with versions newer than the current system version are selected
3. **Ordering**: Selected patches are sorted in ascending version order
4. **Application**: Each patch's `Up()` method is called sequentially
5. **Tracking**: Successfully applied patches are recorded in storage
6. **Version Update**: System version is updated to the latest applied patch version

### Storage

Patches are tracked in MongoDB:
- **Collection**: `patches` (in system database)
- **Version**: Stored separately as the current system version
- **State**: PatchManager grain maintains state of all applied patches

## Writing a Patch

### Basic Structure

Create a patch by implementing `ICanApplyPatch`:

```csharp
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Patches;

public class MyPatch(IStorage storage, ILogger<MyPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(1, 5, 0);

    public async Task Up()
    {
        // Migration logic here
        logger.LogInformation("Applying MyPatch");
        
        // Access storage as needed
        var eventStore = storage.GetEventStore(EventStoreName.System);
        // ... perform migration
    }

    public async Task Down()
    {
        // Rollback logic here (optional but recommended)
        logger.LogInformation("Rolling back MyPatch");
        
        // Reverse the changes made in Up()
    }
}
```

### Patch Naming Conventions

- **Folder structure**: Organize patches by version in `Source/Kernel/Core/Patches/{version}/`
- **File naming**: Use descriptive names (e.g., `RenameReactors.cs`, `MigrateEventSchema.cs`)
- **Class naming**: Match the file name (the `Name` property is auto-derived from the type name)

### Dependencies

Patches can inject any services registered in the DI container:

```csharp
public class ComplexPatch(
    IStorage storage,
    IEventTypes eventTypes,
    ILogger<ComplexPatch> logger) : ICanApplyPatch
{
    public SemanticVersion Version => new(2, 0, 0);
    
    public async Task Up()
    {
        // Use injected services
        await eventTypes.DiscoverAndRegister(EventStoreName.System);
    }
    
    public async Task Down()
    {
        // Rollback
    }
}
```

## Best Practices

### 1. Semantic Logging

Use proper semantic logging with `LoggerMessage` attributes:

```csharp
internal static partial class MyPatchLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting MyPatch migration")]
    internal static partial void StartingMigration(this ILogger<MyPatch> logger);
    
    [LoggerMessage(LogLevel.Information, "Migrated {Count} items")]
    internal static partial void MigratedItems(this ILogger<MyPatch> logger, int count);
}

public class MyPatch(IStorage storage, ILogger<MyPatch> logger) : ICanApplyPatch
{
    public async Task Up()
    {
        logger.StartingMigration();
        // ... migration logic
        logger.MigratedItems(count);
    }
}
```

### 2. Implement Down() for Safety

Always implement `Down()` to support rollback scenarios:

```csharp
public async Task Down()
{
    // Reverse the changes made in Up()
    // This allows safe rollback if needed
}
```

### 3. Handle Idempotency

Patches should be idempotent where possible. The system prevents re-execution, but defensive coding helps:

```csharp
public async Task Up()
{
    var eventStore = storage.GetEventStore(EventStoreName.System);
    var reactors = await eventStore.Reactors.GetAll();
    
    // Filter to only process items that need migration
    var reactorsToMigrate = reactors
        .Where(r => r.Identifier.Value.Contains("OldPattern"))
        .ToList();
    
    if (reactorsToMigrate.Count == 0)
    {
        logger.NothingToMigrate();
        return;
    }
    
    // Proceed with migration
}
```

### 4. Test Thoroughly

Write comprehensive specs for your patches:

```csharp
public class when_applying_my_patch : given.a_my_patch_context
{
    async Task Because() => await _patch.Up();
    
    [Fact] void should_migrate_data() => /* assertion */;
    [Fact] void should_preserve_existing_data() => /* assertion */;
}
```

## Runtime Behavior

### Startup Sequence

1. **Server Initialization**: Chronicle server starts
2. **Patch Discovery**: `PatchManager` grain discovers all `ICanApplyPatch` implementations
3. **Version Check**: Current system version is retrieved from storage
4. **Patch Selection**: Patches newer than current version are selected
5. **Sequential Application**: Patches are applied in ascending version order
6. **Version Update**: System version is updated to latest applied patch
7. **Normal Startup**: Chronicle continues with normal initialization

### Error Handling

If a patch fails:
- The exception is logged and re-thrown
- Startup is halted to prevent running with inconsistent state
- Manual intervention is required to fix the issue
- The patch can be fixed and server restarted

### Version Comparison

Patches are applied based on version comparison:

- **Current: 1.0.0, Patch: 1.5.0** → Patch IS applied
- **Current: 2.0.0, Patch: 1.5.0** → Patch is NOT applied
- **Current: 1.5.0, Patch: 1.5.0** → Patch is NOT applied (equal versions)
- **Current: null, Patch: 1.0.0** → Patch IS applied (treats null as 0.0.0)

## Example: RenameReactors Patch

The `RenameReactors` patch (version 15.3.0) demonstrates a real-world migration:

```csharp
public class RenameReactors(IStorage storage, ILogger<RenameReactors> logger) 
    : ICanApplyPatch
{
    public SemanticVersion Version => new(15, 3, 0);

    public async Task Up()
    {
        logger.StartingPatch();
        
        var systemEventStore = storage.GetEventStore(EventStoreName.System);
        var reactors = await systemEventStore.Reactors.GetAll();
        
        var reactorsToRename = reactors
            .Where(r => r.Identifier.Value.Contains("Grains", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        logger.FoundReactorsToRename(reactorsToRename.Count);
        
        foreach (var reactor in reactorsToRename)
        {
            var currentId = reactor.Identifier;
            var newIdValue = currentId.Value.Replace("Grains", string.Empty, StringComparison.OrdinalIgnoreCase);
            var newId = new ReactorId(newIdValue);
            
            logger.RenamingReactor(currentId, newId);
            await systemEventStore.Reactors.Rename(currentId, newId);
        }
        
        logger.PatchCompleted();
    }

    public async Task Down()
    {
        // Restore "Grains" in reactor names for rollback
        logger.StartingRollback();
        // ... rollback logic
        logger.RollbackCompleted();
    }
}
```

## Troubleshooting

### Patch Not Running

1. **Check version**: Ensure patch version is greater than current system version
2. **Check registration**: Verify patch implements `ICanApplyPatch` and is in correct namespace
3. **Check logs**: Look for patch discovery and application logs at startup

### Patch Failing

1. **Review exception**: Check startup logs for detailed error information
2. **Test locally**: Run patch specs to verify logic
3. **Check storage**: Ensure storage connections are working
4. **Verify state**: Check if system is in expected state before migration

### Need to Re-run Patch

If you need to re-run a patch (e.g., during development):

1. Remove patch record from `patches` collection in MongoDB
2. Optionally reset system version if needed
3. Restart server

**Warning**: Only do this in development environments. Production systems should use new patch versions.

## Migration from Older Versions

When upgrading Chronicle from versions without the patch system:

1. System version defaults to `0.0.0` (or `SemanticVersion.NotSet`)
2. All patches will be discovered and applied in order
3. After successful application, system version is set to latest patch version
4. Future upgrades will only apply newer patches

## Summary

The patch system provides a robust, automated way to handle breaking changes during Chronicle upgrades. By following the conventions and best practices outlined here, you can write safe, testable patches that keep Chronicle systems properly migrated across versions.
