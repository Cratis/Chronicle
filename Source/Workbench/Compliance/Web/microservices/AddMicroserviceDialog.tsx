// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { TextField } from '@fluentui/react';
import { IModalProps } from '@cratis/fluentui';

export type AddMicroserviceDialogResult = {
    name: string;
};

export const AddMicroserviceDialog = (props: IModalProps<any, AddMicroserviceDialogResult>) => {
    const [name, setName] = useState('');

    props.onClose(result => {
        setName('');

        return {
            name
        };
    });

    return (
        <TextField label="Name" value={name} onChange={(ev, value) => setName(value!)} />
    );
};
