// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useCallback, useState } from 'react';
import { ModalButtons } from './Modal';
import { ModalContext, OnCloseCallback } from './ModalContext';

export enum ModalResult {
    success,
    failed,
    dismissed
}

export type ShowModal<T = {}> = (input?: T) => void;
export type CloseModal = (result: ModalResult) => void;
export type ModalClosed<T> = (result: ModalResult, output?: T) => void;

export interface IModalProps<TInput = {}, TOutput = {}> {
    children?: JSX.Element | JSX.Element[];
    input?: TInput;
    onClose: (callback: OnCloseCallback<TOutput>) => void;
}

export type Content<TInput, TOutput> = React.FunctionComponent<IModalProps<TInput, TOutput>> | React.ComponentClass<IModalProps<TInput, TOutput>> | string;

/**
 * Hook for adding a modal dialog. Requires the use of the <ModalProvider/> higher in the stack.
 * @param {string} title Title of the modal dialog.
 * @param {ModalButtons} buttons Button types to add to the footer.
 * @param {Content<TInput, TOutput>} content The content to have in the dialog.
 * @param {ModalClosed<TOutput>?} onClose Optional callback to call when dialog is closed.
 * @returns Tuple containing {@link ShowModal<TInput>} and {@link CloseModal}.
 */
export function useModal<TInput = {}, TOutput = {}>(
    title: string,
    buttons: ModalButtons,
    content: Content<TInput, TOutput>,
    onClose?: ModalClosed<TOutput>): [ShowModal<TInput>, CloseModal] {
    const context = React.useContext(ModalContext);

    if (!context) {
        throw new Error('useModal must be used within a ModalProvider');
    }

    const closeModal = () => {

    };

    const showModal = (input?: TInput) => {
        let contentInstance: React.ReactElement;

        if (content.constructor == String) {
            contentInstance = React.createElement("div", {} as any, content);
        } else {
            const modalProps: IModalProps<TInput, TOutput> = {
                input,
                onClose: context.onClose!
            };
            contentInstance = React.createElement(content, modalProps, null);
        }

        context.showModal?.({
            title,
            buttons,
            content: contentInstance,
            closeModal: context.closeModal,
            modalClosed: onClose
        });
    };

    return [showModal, closeModal];
}
