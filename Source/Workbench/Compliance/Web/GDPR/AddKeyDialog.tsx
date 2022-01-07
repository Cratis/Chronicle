// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IModalProps } from '@cratis/fluentui';
import { Guid } from '@cratis/fundamentals';
import { Stack, TextField, IconButton, ITextFieldStyles } from '@fluentui/react';
import { useState } from 'react';

interface AddKeyDialogResult {
    identifier: string;
}

const textFieldStyles: Partial<ITextFieldStyles> = {
    icon: {
        pointerEvents: 'initial',
        cursor: 'pointer'
    }
};


export const AddKeyDialog = (props: IModalProps<{}, AddKeyDialogResult>) => {
    const [identifier, setIdentifier] = useState('');

    const generate = () => {
        setIdentifier(Guid.create().toString());
    };

    props.onClose(result => {
        return {
            identifier
        };
    });

    return (
        <TextField
            label="Identifier for person"
            styles={textFieldStyles}
            width={400}
            value={identifier}
            onChange={(ev, value) => {
                setIdentifier(value!);
            }}
            iconProps={{
                title: 'Generate new unique identifier',
                iconName: 'CircleAddition', onClick: (ev) => {
                    ev.stopPropagation();
                    generate();
                }
            }} />
    );
};
