// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import { DialogButtons } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState, type ChangeEvent } from 'react';

export interface QueryModalProps {
    isOpen: boolean;
    closeModal: () => void;
    newFolder: (folder: string) => void;
}

export const QueryModal = (props: QueryModalProps) => {
    const { isOpen, closeModal, newFolder } = props;

    const [folderName, setFolderName] = useState('');

    const handleModalClose = () => {
        setFolderName('');
        closeModal();
    };

    const addNewFolder = () => {
        setFolderName('');
        newFolder(folderName);
    };

    return (
        <Dialog
            title='Queries'
            visible={isOpen}
            width='50vw'
            buttons={DialogButtons.YesNo}
            onConfirm={addNewFolder}
            onCancel={handleModalClose}
        >
            <InputText
                value={folderName}
                onChange={(event: ChangeEvent<HTMLInputElement>) =>
                    setFolderName(event.target.value)
                }
            />
            <p className='m-0'>
                Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod
                tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim
                veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea
                commodo consequat. Duis aute irure dolor in reprehenderit in voluptate
                velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat
                cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id
                est laborum.
            </p>
        </Dialog>
    );
};



