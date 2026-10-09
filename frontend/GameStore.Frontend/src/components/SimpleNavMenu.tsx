import React from 'react';
import BrandLogo from './BrandLogo';

const SimpleNavMenu: React.FC = () => {
    return (
        <nav className="navbar navbar-expand-lg bg-body-tertiary">
            <div className="container">
                <BrandLogo />
            </div>
        </nav>
    );
};

export default SimpleNavMenu;
