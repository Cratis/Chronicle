// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultButton, Dialog, DialogFooter, DialogType, IDialogContentProps, PrimaryButton } from '@fluentui/react';
import { CloseModal, ModalClosed, ModalResult } from './useModal';


export enum ModalButtons {
    None = 1,
    OkCancel,
    YesNo
}

export interface IModalRenderProps {
    buttons: ModalButtons;
    title?: string | JSX.Element;
    content?: string | JSX.Element;
    minWidth?: string | number;
    closeModal?: CloseModal;
    modalClosed?: ModalClosed<any>;
}


export const Modal = (props: IModalRenderProps) => {
    const success = () => {
        props.closeModal?.(ModalResult.Success);
    };

    const dismiss = () => {
        props.closeModal?.(ModalResult.Dismissed);
    };

    const contentProps: IDialogContentProps = {
        type: DialogType.normal,
        title: props.title,
        closeButtonAriaLabel: 'Close'
    };

    return (
        <Dialog
            minWidth={props.minWidth || 600}
            hidden={false}
            onDismiss={dismiss}
            dialogContentProps={contentProps}>

            {props.content}

            {(() => {
                switch (props.buttons) {
                    case ModalButtons.OkCancel:
                        return (
                            <DialogFooter>
                                <PrimaryButton onClick={success} text="Ok" />
                                <DefaultButton onClick={dismiss} text="Cancel" />
                            </DialogFooter>
                        );

                    case ModalButtons.YesNo:
                        return (
                            <DialogFooter>
                                <PrimaryButton onClick={success} text="Yes" />
                                <DefaultButton onClick={dismiss} text="No" />
                            </DialogFooter>
                        );

                    default:
                        return <></>;
                }
            })()}
        </Dialog>
    );
};



