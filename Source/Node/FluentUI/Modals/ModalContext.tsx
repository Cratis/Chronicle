// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useMemo, useState } from 'react';
import { IModalRenderProps, Modal, ModalButtons } from './Modal';
import { CloseModal, ModalClosed, ModalResult, ShowModal } from './useModal';

export const ModalContext = React.createContext<IModalContext>({});

export type OnCloseCallback<T> = (result: ModalResult) => T;

export interface IModalContext {
    children?: JSX.Element | JSX.Element[];
    onClose?: (callback: OnCloseCallback<any>) => void;
    showModal?: (props: IModalRenderProps) => void;
    closeModal?: CloseModal;
}

let currentCloseCallback: OnCloseCallback<any> | undefined;
let modalProps: IModalRenderProps | undefined;

export const ModalProvider = (props: IModalContext) => {
    const [visible, setVisible] = useState<Boolean>(false);
    const showModal = (modal: IModalRenderProps) => {
        modalProps = modal;
        setVisible(true);
    };
    const closeModal = (result: ModalResult) => {
        const output = currentCloseCallback?.(result);
        modalProps?.modalClosed?.(result, output);
        setVisible(false);
        modalProps = undefined;
        currentCloseCallback = undefined;
    };
    const onClose = (callback: OnCloseCallback<any>) => {
        currentCloseCallback = callback;
    };

    return (
        <ModalContext.Provider value={{ showModal, closeModal, onClose }} {...props} >
            {props.children}
            {visible && modalProps && <Modal {...modalProps} />}
        </ModalContext.Provider>
    );
};
