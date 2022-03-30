// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useEffect, useState } from 'react';
import { Command } from './Command';
import { CommandResult } from './CommandResult';
import { CommandResults } from './CommandResults';
import { CommandTrackerImplementation } from './CommandTrackerImplementation';
import { ICommandTracker } from './ICommandTracker';


const defaultCommandTrackerContext: ICommandTracker = {
    addCommand: () => { },
    execute: async () => {
        return new CommandResults(new Map());
    },
    hasChanges: false,
};

export const CommandTrackerContext = React.createContext<ICommandTracker>(defaultCommandTrackerContext);

export type CommandTrackerChanged = (hasChanges: boolean) => void;
export type CommandTrackerExecute = () => Promise<Map<Command, CommandResult>>;

export type AddCommand = (command: Command) => void;

export interface ICommandTrackerProps {
    children?: JSX.Element | JSX.Element[];
    setHasChanges?: CommandTrackerChanged;
}

export const CommandTracker = (props: ICommandTrackerProps) => {
    const [hasChanges, setHasChanges] = useState(false);
    const [commandTracker, setCommandTracker] = useState<ICommandTracker>(defaultCommandTrackerContext);

    useEffect(() => {
        const commandTrackerImplementation = new CommandTrackerImplementation((value) => {
            setHasChanges(value);
        });
        setCommandTracker(commandTrackerImplementation);
    }, []);

    if (commandTracker) {
        (commandTracker as any).hasChanges = hasChanges;
    }

    return (
        <CommandTrackerContext.Provider value={{
            addCommand: (command) => commandTracker!.addCommand(command),
            execute: () => commandTracker.execute(),
            hasChanges
        }}>
            {props.children}
        </CommandTrackerContext.Provider>
    );
};
