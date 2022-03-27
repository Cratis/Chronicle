// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@aksio/cratis-applications-frontend/commands';
import React, { useCallback, useState } from 'react';

export const CommandTrackerContext = React.createContext<ICommandTrackerContext>({});

export type CommandTrackerChangedChanged = (hasChanges: boolean) => void;

export type AddCommand = (command: Command) => void;

export interface ICommandTrackerContext {
    children?: JSX.Element | JSX.Element[];
    addCommand?: AddCommand;
    onChange?: CommandTrackerChangedChanged;
}

export const CommandTracker = (props: ICommandTrackerContext) => {
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

        if (hasCommandChanges) {
            props.onChange?.(hasChanges);
        }
    }, []);

    const addCommand = (command: Command) => {
        if( commands.some(_ => _ == command) ) {
            return;
        }

        commands.push(command);
        command.onPropertyChanged(propertyChanged);
    };

    return (
        <CommandTrackerContext.Provider value={{ addCommand }} {...props}>
            {props.children}
        </CommandTrackerContext.Provider>
    );
};
