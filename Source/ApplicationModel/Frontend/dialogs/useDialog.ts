// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { DialogResult } from './DialogResult';

export interface IDialogProps<TInput = {}, TOutput = {}> {
    visible: boolean;
    input: TInput;
    onClose: DialogClosed<TOutput>
}

export type ShowDialog<T = {}> = (input?: T) => void;
export type DialogClosed<T> = (result: DialogResult, output?: T) => void;

export function useDialog<TInput = {}, TOutput = {}>(onClose: DialogClosed<TOutput>): [ShowDialog<TInput>, IDialogProps<TInput, TOutput>] {
    const [visible, setVisible] = useState(false);
    const [input, setInput] = useState({});

    const props = {
        visible,
        input,
        onClose: (result, output) => {
            setVisible(false);
            onClose(result, output);
        }
    } as IDialogProps<TInput, TOutput>;

    const show = (input?: TInput) => {
        if (input) {
            setInput(input);
        }
        setVisible(true);
    };

    return [
        show,
        props
    ];
}
