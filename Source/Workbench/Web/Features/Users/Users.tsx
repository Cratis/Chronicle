// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Button } from 'primereact/button';
import { ChangePassword } from './ChangePassword';
import { useDialog } from '@cratis/arc.react/dialogs';
import { RequirePasswordChange } from 'Api/Users';

export const Users = () => {
    const [ChangePasswordDialog, showChangePasswordDialog] = useDialog(ChangePassword);

    // Mock user data - in real implementation, this would come from the API via observable query
    const users = [
        { id: '00000000-0000-0000-0000-000000000001', username: 'admin', hasLoggedIn: false, passwordChangeRequired: true }
    ];

    const handleForcePasswordChange = async (userId: string) => {
        await RequirePasswordChange({ userId });
    };

    return (
        <div className='m-4'>
            <h2 className='text-4xl m-3'>Users</h2>
            
            <div className='flex flex-wrap w-full'>
                <table className='w-full'>
                    <thead>
                        <tr>
                            <th className='text-left p-2'>Username</th>
                            <th className='text-left p-2'>Status</th>
                            <th className='text-left p-2'>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map((user) => (
                            <tr key={user.id}>
                                <td className='p-2'>{user.username}</td>
                                <td className='p-2'>
                                    {user.passwordChangeRequired ? 'Password Change Required' : 'Active'}
                                </td>
                                <td className='p-2'>
                                    <Button 
                                        label="Force Password Change" 
                                        onClick={() => handleForcePasswordChange(user.id)}
                                        className='mr-2'
                                    />
                                    <Button 
                                        label="Change Password" 
                                        onClick={() => showChangePasswordDialog({ userId: user.id })}
                                    />
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            
            <ChangePasswordDialog />
        </div>
    );
};
