import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';

import { tw } from 'typewind';

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

    const footerContent = (
        <div>
            <Button label='No' icon='pi pi-times' onClick={handleModalClose} />
            <Button label='Yes' icon='pi pi-check' onClick={addNewFolder} autoFocus />
        </div>
    );

    return (
        <Dialog
            header='Queries'
            visible={isOpen}
            style={{ width: '50vw' }}
            onHide={handleModalClose}
            footer={footerContent}>
            <InputText
                value={folderName}
                onChange={(evt: React.ChangeEvent<HTMLInputElement>) =>
                    setFolderName(evt.target.value)
                }
            />
            <p className={tw.m_0}>
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



