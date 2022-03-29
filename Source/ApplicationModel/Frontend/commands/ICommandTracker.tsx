// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICommand } from './ICommand';
import { CommandResults } from './CommandResults';

export interface ICommandTracker {
    readonly hasChanges: boolean;
    addCommand(command: ICommand): void;
    execute(): Promise<CommandResults>;
}
