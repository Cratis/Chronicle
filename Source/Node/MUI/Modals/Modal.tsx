// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button, Dialog, DialogActions, DialogContent, DialogTitle } from '@mui/material';
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
        props.closeModal?.(ModalResult.success);
    };

    const dismiss = () => {
        props.closeModal?.(ModalResult.dismissed);
    };

    return (
        <Dialog
            open={true}
            onClose={dismiss}>

            <DialogTitle>
                {props.title}
            </DialogTitle>

            <DialogContent>
                {props.content}
            </DialogContent>

            {(() => {
                switch (props.buttons) {
                    case ModalButtons.OkCancel:
                        return (
                            <DialogActions>
                                <Button onClick={success}>Ok</Button>
                                <Button onClick={dismiss}>Cancel</Button>
                            </DialogActions>
                        );

                    case ModalButtons.YesNo:
                        return (
                            <DialogActions>
                                <Button onClick={success}>Yes</Button>
                                <Button onClick={dismiss}>No</Button>
                            </DialogActions>
                        );

                    default:
                        return <></>;
                }
            })()}
        </Dialog>
    );
};
