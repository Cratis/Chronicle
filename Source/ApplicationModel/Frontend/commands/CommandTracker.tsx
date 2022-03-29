// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@aksio/cratis-applications-frontend/commands';
import React, { useCallback, useState } from 'react';
import { CommandResult } from './CommandResult';


export interface ICommandTracker {
    addCommand: AddCommand;
    hasChanges: boolean;
    execute: CommandTrackerExecute;
}

export const CommandTrackerContext = React.createContext<ICommandTracker>({
    addCommand: () => { },
    execute: async () => {
        return new Map();
    },
    hasChanges: false,
});

export type CommandTrackerChanged = (hasChanges: boolean) => void;
export type CommandTrackerExecute = () => Promise<Map<Command, CommandResult>>;

export type AddCommand = (command: Command) => void;

export interface ICommandTrackerProps {
    children?: JSX.Element | JSX.Element[];
    setHasChanges?: CommandTrackerChanged;
}

export const CommandTracker = (props: ICommandTrackerProps) => {
    const commands: Command[] = [];
    const [hasChanges, setHasChanges] = useState(false);

    const propertyChanged = useCallback((property) => {
        let hasCommandChanges = false;
        commands.forEach(command => {
            if (command.hasChanges) {
                hasCommandChanges = true;
            }
        });

        setHasChanges(hasCommandChanges);
    }, []);

    const addCommand = (command: Command) => {
        if (commands.some(_ => _ == command)) {
            return;
        }

        commands.push(command);
        command.onPropertyChanged(propertyChanged);
    };

    const execute = async () => {
        const commandsToCommandResult = new Map();

        for( const command of commands.filter(_ => _.hasChanges === true) )
        {
            const commandResult = await command.execute();
            commandsToCommandResult.set(command, commandResult);
        }
        return commandsToCommandResult;
    };

    return (
        <CommandTrackerContext.Provider value={{ hasChanges, addCommand, execute, }}>
            {props.children}
        </CommandTrackerContext.Provider>
    );
};



