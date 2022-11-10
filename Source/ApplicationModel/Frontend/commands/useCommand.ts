// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@aksio/cratis-fundamentals';
import { useState, useEffect, useCallback } from 'react';
import { Command } from '@aksio/cratis-applications-frontend/commands';
import React from 'react';
import { CommandTrackerContext } from './CommandTracker';

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

    const context = React.useContext(CommandTrackerContext);
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
