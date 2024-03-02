// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppBar, Box, Button, Container, IconButton, Menu, MenuItem, Toolbar, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import BorderClear from '@mui/icons-material/BorderClear';
import React from 'react';

type page = {
    name: string;
    route: string
}

const pages: page[] = [
    { name: 'Home', route: '/' },
    { name: 'Event Store', route: '/event-store' },
    { name: 'Metrics', route: '/metrics' },
    { name: 'Clients', route: '/clients' },
    { name: 'Configuration', route: '/configuration' }];

export const Header = () => {
    const navigate = useNavigate();

    const [anchorElNav, setAnchorElNav] = React.useState<null | HTMLElement>(null);

    const handleOpenNavMenu = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorElNav(event.currentTarget);
    };

    const handleCloseNavMenu = () => {
        setAnchorElNav(null);
    };

    const navigateToPage = (page) => {
        handleCloseNavMenu();
        navigate(page.route);
    };

    return (
        <AppBar position='static' color='primary'>
            <Container maxWidth='xl'>
                <Toolbar disableGutters>
                    <BorderClear sx={{ display: { xs: 'none', md: 'flex' }, mr: 1 }} />
                    <Typography
                        variant='h5'
                        noWrap
                        component='a'
                        href=''
                        sx={{
                            mr: 2,
                            display: { xs: 'flex', md: 'none' },
                            flexGrow: 1,
                            fontFamily: 'monospace',
                            fontWeight: 700,
                            letterSpacing: '.3rem',
                            color: 'inherit',
                            textDecoration: 'none'
                        }}>
                        Cratis
                    </Typography>
                    <Box sx={{ flexGrow: 1, display: { xs: 'none', md: 'flex' } }}>
                        {pages.map((page) => (
                            <Button
                                key={page.name}
                                onClick={() => navigateToPage(page)}
                                sx={{ my: 2, color: 'white', display: 'block' }}>
                                {page.name}
                            </Button>
                        ))}
                    </Box>
                </Toolbar>
            </Container>
        </AppBar>
    );
};
