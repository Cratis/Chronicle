// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Dialog } from 'primereact/dialog';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { ChangePassword as ChangePasswordCommand } from 'Api/Users';

export interface ChangePasswordProps {
    userId?: string;
    onClose?: () => void;
}

export const ChangePassword = ({ userId, onClose }: ChangePasswordProps) => {
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');

    const handleSubmit = async () => {
        setError('');

        if (!newPassword || !confirmPassword) {
            setError('Please enter and confirm your password');
            return;
        }

        if (newPassword !== confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        if (newPassword.length < 8) {
            setError('Password must be at least 8 characters');
            return;
        }

        try {
            await ChangePasswordCommand({ 
                userId: userId || '00000000-0000-0000-0000-000000000001', 
                newPassword 
            });
            
            setNewPassword('');
            setConfirmPassword('');
            onClose?.();
        } catch (err) {
            setError('Failed to change password');
        }
    };

    return (
        <Dialog
            header="Change Password"
            visible={true}
            style={{ width: '450px' }}
            onHide={() => onClose?.()}
        >
            <div className='flex flex-col gap-3'>
                <div>
                    <label htmlFor="newPassword" className='block mb-2'>New Password</label>
                    <InputText
                        id="newPassword"
                        type="password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        className='w-full'
                        autoFocus
                    />
                </div>

                <div>
                    <label htmlFor="confirmPassword" className='block mb-2'>Confirm Password</label>
                    <InputText
                        id="confirmPassword"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        className='w-full'
                    />
                </div>

                {error && (
                    <div className='text-red-500'>{error}</div>
                )}

                <div className='flex justify-end gap-2 mt-4'>
                    <Button label="Cancel" onClick={() => onClose?.()} className='p-button-text' />
                    <Button label="Change Password" onClick={handleSubmit} />
                </div>
            </div>
        </Dialog>
    );
};
