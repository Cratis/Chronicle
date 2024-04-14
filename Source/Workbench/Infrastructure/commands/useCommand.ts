// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from 'Infrastructure';
import { useState, useEffect, useCallback } from 'react';
import { Command } from './Command';
import React from 'react';
import { CommandScopeContext } from './CommandScope';

export type SetCommandValues<TCommandContent> = (command: TCommandContent) => void;
export type ClearCommandValues = () => void;

export function useCommand<TCommand extends Command, TCommandContent>(commandType: Constructor<TCommand>, initialValues?: TCommandContent): [TCommand, SetCommandValues<TCommandContent>, ClearCommandValues] {
    const instance = new commandType();
    const [command, setCommand] = useState<TCommand>(instance);
    const [hasChanges, setHasChanges] = useState(false);

    const propertyChangedCallback = useCallback(property => {
        if (command.hasChanges !== hasChanges) {
            setHasChanges(command.hasChanges);
        }
    }, []);

    useEffect(() => {
        if (initialValues) {
            instance.setInitialValues(initialValues);
        }

        instance.onPropertyChanged(propertyChangedCallback, instance);
    }, []);

    const context = React.useContext(CommandScopeContext);
    context.addCommand?.(command!);

    const setCommandValues = (values: TCommandContent) => {
        command!.properties.forEach(property => {
            if (values[property] !== undefined && values[property] != null) {
                command![property] = values[property];
            }
        });
    };

    const clearCommandValues = () => {
        command!.properties.forEach(property => {
            command![property] = undefined;
        });
    };

    return [command!, setCommandValues, clearCommandValues];
}
