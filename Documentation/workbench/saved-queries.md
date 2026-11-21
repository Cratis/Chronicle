---
title: Saved queries and folders
description: Who can see, change, and delete saved event sequence queries and folders in Chronicle Workbench, and what the Everyone scope does and does not allow.
---
Chronicle Workbench lets you save an event sequence query and file it in a folder. Every saved query and folder has an owner and a scope. This page explains how those two decide what each user can do.

## Owner and scope

The owner is the user who first saved the item. Chronicle takes it from the signed-in user and never from the request, so a client cannot choose it. Saving an item that already exists keeps its owner, so saving over someone else's item can never take it over.

The scope controls who can see and run the item:

| Scope      | Who can see and run it |
|------------|------------------------|
| `User`     | Only the owner         |
| `Everyone` | Every user             |

## Who can change or delete

Only the owner or an administrator can update or delete a saved query or folder. This holds for both scopes: `Everyone` makes an item visible, not editable. A user who wants to adapt a shared query saves a copy of their own.

| Caller        | Item they own | Another user's `User` item | Another user's `Everyone` item |
|---------------|---------------|----------------------------|--------------------------------|
| Regular user  | Allowed       | Refused                    | Refused                        |
| Administrator | Allowed       | Allowed                    | Allowed                        |

The owner is compared exactly: owners that differ only by letter case or a trailing space are different owners, on every storage provider. The owner never changes when an administrator edits an item.

A refused change returns one of these messages and leaves the item untouched:

- `The saved query belongs to another owner.`
- `The saved query folder belongs to another owner.`

The check is repeated when Chronicle writes the change, so an item that changes owner between the check and the write is refused rather than overwritten.

## Deleting a folder

Deleting a folder in the Workbench deletes each query and folder in it one by one. Items you do not own are refused and stay where they are, along with the folders that still contain them.
