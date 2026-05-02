# Vertical Slice Patterns

## State Change — full example

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyApp.Projects.Registration;

// ─── Concepts ───────────────────────────────────────────────────────────────

/// <summary>
/// Represents the unique identifier of a project.
/// </summary>
public record ProjectId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets a sentinel value for an unset identifier.
    /// </summary>
    public static readonly ProjectId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="ProjectId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator ProjectId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ProjectId"/> to an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id">The <see cref="ProjectId"/> to convert.</param>
    public static implicit operator EventSourceId(ProjectId id) => new(id.Value.ToString());

    /// <summary>
    /// Creates a new <see cref="ProjectId"/> with a unique value.
    /// </summary>
    /// <returns>A new <see cref="ProjectId"/>.</returns>
    public static ProjectId New() => new(Guid.NewGuid());
}

/// <summary>
/// Represents the name of a project.
/// </summary>
public record ProjectName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a sentinel value for an unset name.
    /// </summary>
    public static readonly ProjectName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="ProjectName"/>.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    public static implicit operator ProjectName(string value) => new(value);

    /// <summary>
    /// Implicitly extracts the underlying <see cref="string"/> value.
    /// </summary>
    /// <param name="name">The <see cref="ProjectName"/> to convert.</param>
    public static implicit operator string(ProjectName name) => name.Value;
}

// ─── Command ────────────────────────────────────────────────────────────────

/// <summary>
/// Command to register a new project.
/// </summary>
[Command]
public record RegisterProject(ProjectId ProjectId, ProjectName Name)
{
    /// <summary>
    /// Produces a <see cref="ProjectRegistered"/> event.
    /// </summary>
    /// <returns>The <see cref="ProjectRegistered"/> event.</returns>
    public ProjectRegistered Handle() => new(Name);
}

// ─── Constraint ─────────────────────────────────────────────────────────────

/// <summary>
/// Prevents two projects from being registered with the same name.
/// </summary>
public class UniqueProjectNameConstraint : IConstraint
{
    /// <inheritdoc/>
    public void Define(IConstraintBuilder builder) => builder
        .Unique(unique => unique.On<ProjectRegistered>(e => e.Name));
}

// ─── Event ──────────────────────────────────────────────────────────────────

/// <summary>
/// Raised when a new project has been successfully registered.
/// </summary>
[EventType]
public record ProjectRegistered(ProjectName Name);
```

**Key rules for State Change slices:**
- `[Command]` attribute marks the command record — there is no `ICommand` interface
- `Handle()` RETURNS the event — it never calls `IEventLog` or `eventLog.Append()`
- `[EventType]` has **no arguments** — the event type name is resolved automatically
- The event source ID is resolved in order: `ICanProvideEventSourceId` > `EventSourceId` property > `[Key]` attribute
- Constraints (`IConstraint`) live in the same slice file as the command
- The read model and projection live in the **State View** slice, not here

---

## State View — full example

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyApp.Projects.Listing;

// ─── Read Model (model-bound projection — no separate class needed) ──────────

/// <summary>
/// Represents a project in the listing read model.
/// </summary>
[ReadModel]
[FromEvent<Registration.ProjectRegistered>]
public record Project(
    [Key] ProjectId Id,
    ProjectName Name)
{
    /// <summary>
    /// Observes all projects in the collection.
    /// </summary>
    /// <param name="collection">The <see cref="IMongoCollection{Project}"/> to observe.</param>
    /// <returns>An observable subject of all projects.</returns>
    public static ISubject<IEnumerable<Project>> AllProjects(IMongoCollection<Project> collection) =>
        collection.Observe();
}
```

**Key rules for State View slices:**
- The read model is decorated with `[ReadModel]` — needed for the static observable query API
- **Model-bound projection (preferred):** Add `[FromEvent<TEvent>]` at class level for auto-mapping — no separate `IProjectionFor<T>` class needed
- Mark the primary key property with `[Key]` from `Cratis.Chronicle.Keys`
- Use `[SetFrom<TEvent>]` / `[AddFrom<TEvent>]` / `[SubtractFrom<TEvent>]` for explicit property-level mapping
- Use `[ChildrenFrom<TEvent>]` for nested child collections, `[Join<TEvent>]` for cross-event enrichment
- Query methods are **static methods** on the read model record itself
- `Observe()` returns an `ISubject<IEnumerable<T>>` for live updates; use `ObserveWithPaging(...)` for paged results

**When to use fluent `IProjectionFor<T>` instead:**
- Projection logic is too complex for attributes (e.g. conditional branching)
- You prefer to separate projection definition from the read model type

```csharp
// Fluent alternative — still correct, use for complex cases
public class ProjectProjection : IProjectionFor<Project>
{
    public void Define(IProjectionBuilderFor<Project> builder) => builder
        .From<Registration.ProjectRegistered>();     // AutoMap is on by default
}
```

---

## Automation (Reactor) — full example

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyApp.Projects.Notifications;

/// <summary>
/// Sends a notification when a project is registered.
/// </summary>
/// <param name="notifications">The notification service.</param>
public class ProjectRegisteredNotifier(INotificationService notifications) : IReactor
{
    /// <summary>
    /// Reacts to <see cref="Registration.ProjectRegistered"/> events.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task ProjectRegistered(Registration.ProjectRegistered @event, EventContext context) =>
        await notifications.Notify($"Project '{@event.Name}' was registered.");
}
```

**Key rules:**
- Reactors implement `IReactor` — a marker interface with **no methods**
- The method name MUST match the event type name exactly (by convention)
- Reactors MUST be idempotent — they can be called more than once per event
- Use the event data directly — do not query the read model inside the handler
- To trigger further commands, inject and call `ICommandPipeline` — never use `IEventLog` directly

---

## Translation — full example

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MyApp.Projects.StockKeeping;

/// <summary>
/// Reacts to reservation events and decreases stock accordingly.
/// </summary>
/// <param name="stockKeeper">The stock keeper service.</param>
/// <param name="commandPipeline">The command pipeline for executing commands.</param>
public class StockKeeping(IStockKeeper stockKeeper, ICommandPipeline commandPipeline) : IReactor
{
    /// <summary>
    /// Handles a <see cref="BookReserved"/> event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The event context.</param>
    public async Task BookReserved(BookReserved @event, EventContext context) =>
        await commandPipeline.Execute(new DecreaseStock(@event.Isbn, await stockKeeper.GetStock(@event.Isbn)));
}

/// <summary>
/// Command to decrease available stock of a book.
/// </summary>
[Command]
public record DecreaseStock(ISBN Isbn, BookStock StockBeforeDecrease)
{
    /// <summary>
    /// Produces a <see cref="StockDecreased"/> event.
    /// </summary>
    /// <returns>The <see cref="StockDecreased"/> event.</returns>
    public StockDecreased Handle() => new(Isbn, StockBeforeDecrease);
}

/// <summary>
/// Raised when the available stock of a book has decreased.
/// </summary>
[EventType]
public record StockDecreased(ISBN Isbn, BookStock StockBeforeDecrease);
```

---

## Frontend — complete component examples

### Listing component with paging

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { AllProjects } from './AllProjects';

const pageSize = 10;

export const Listing = () => {
    const [result, , setPage] = AllProjects.useWithPaging(pageSize);

    return (
        <DataTable
            lazy paginator
            value={result.data}
            rows={pageSize}
            totalRecords={result.paging.totalItems}
            alwaysShowPaginator={false}
            first={result.paging.page * pageSize}
            onPage={event => setPage(event.page ?? 0)}
            scrollable scrollHeight="flex"
            emptyMessage="No projects found.">
            <Column field="name" header="Name" />
        </DataTable>
    );
};
```

### CommandDialog for state-change commands

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputText } from 'primereact/inputtext';
import { RegisterProject } from './RegisterProject';

export const AddProject = ({ closeDialog }: DialogProps) => {
    const [name, setName] = useState('');

    return (
        <CommandDialog
            command={RegisterProject}
            visible
            header="Add Project"
            width="32rem"
            confirmLabel="Add"
            cancelLabel="Cancel"
            onBeforeExecute={(values) => {
                values.name = name;
                return values;
            }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <CommandDialog.Fields>
                <InputText
                    value={name}
                    onChange={event => setName(event.target.value)}
                    autoFocus
                />
            </CommandDialog.Fields>
        </CommandDialog>
    );
};
```

### Composition page

```tsx
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialog } from '@cratis/arc.react/dialogs';
import { Menubar } from 'primereact/menubar';
import { MenuItem } from 'primereact/menuitem';
import * as mdIcons from 'react-icons/md';
import { Page } from '../../Core/Page';
import { AddProject } from './Registration/AddProject';
import { Listing } from './Listing/Listing';

export const Projects = () => {
    const [AddProjectDialog, showAddProjectDialog] = useDialog(AddProject);

    const menuItems: MenuItem[] = [
        {
            label: 'Add Project',
            icon: mdIcons.MdAdd,
            command: async () => { await showAddProjectDialog(); }
        }
    ];

    return (
        <Page title="Projects">
            <Menubar model={menuItems} />
            <Listing />
            <AddProjectDialog />
        </Page>
    );
};
```
