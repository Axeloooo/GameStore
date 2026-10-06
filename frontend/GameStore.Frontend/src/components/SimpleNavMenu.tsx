import React from 'react';
import { NavLink } from 'react-router-dom';

const SimpleNavMenu: React.FC = () => {
    return (
        <nav className="navbar navbar-expand-lg bg-body-tertiary" data-bs-theme="dark">
            <div className="container">
                <NavLink className="navbar-brand mb-0 h1" to="/">Game Store</NavLink>
            </div>
        </nav>
    );
};

export default SimpleNavMenu;
