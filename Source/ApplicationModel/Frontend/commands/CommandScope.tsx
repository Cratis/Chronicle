// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useEffect, useState } from 'react';
import { Command } from './Command';
import { CommandResult } from './CommandResult';
import { CommandResults } from './CommandResults';
import { CommandScopeImplementation } from './CommandScopeImplementation';
import { ICommandScope } from './ICommandScope';


const defaultCommandScopeContext: ICommandScope = {
    addCommand: () => { },
    execute: async () => {
        return new CommandResults(new Map());
    },
    hasChanges: false,
    revertChanges: () => { }
};

export const CommandScopeContext = React.createContext<ICommandScope>(defaultCommandScopeContext);

export type CommandScopeChanged = (hasChanges: boolean) => void;
export type CommandScopeExecute = () => Promise<Map<Command, CommandResult>>;

export type AddCommand = (command: Command) => void;

export interface ICommandScopeProps {
    children?: JSX.Element | JSX.Element[];
    setHasChanges?: CommandScopeChanged;
}

export const CommandScope = (props: ICommandScopeProps) => {
    const [hasChanges, setHasChanges] = useState(false);
    const [commandScope, setCommandScope] = useState<ICommandScope>(defaultCommandScopeContext);

    useEffect(() => {
        const commandScopeImplementation = new CommandScopeImplementation((value) => {
            setHasChanges(value);
        });
        setCommandScope(commandScopeImplementation);
    }, []);

    if (commandScope) {
        (commandScope as any).hasChanges = hasChanges;
    }

    return (
        <CommandScopeContext.Provider value={{
            addCommand: (command) => commandScope!.addCommand(command),
            execute: () => commandScope.execute(),
            revertChanges: () => commandScope.revertChanges(),
            hasChanges
        }}>
            {props.children}
        </CommandScopeContext.Provider>
    );
};
