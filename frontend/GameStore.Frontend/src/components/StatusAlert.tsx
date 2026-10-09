import React from 'react';

type StatusVariant = 'danger' | 'warning' | 'success';

interface StatusAlertProps {
    variant: StatusVariant;
    className?: string;
    children: React.ReactNode;
}

// Colour is never the only signal: each variant has its own icon shape and a
// screen-reader label. The icons are small inline SVGs (24px grid, 2px stroke).
const icons: Record<StatusVariant, { label: string; paths: React.ReactNode }> = {
    danger: {
        label: 'Error',
        paths: (<><circle cx="12" cy="12" r="9" /><path d="M12 7.5v5.5M12 16.5v.01" /></>),
    },
    warning: {
        label: 'Notice',
        paths: (<><path d="M12 3.5 22 20H2z" /><path d="M12 10v5M12 17.5v.01" /></>),
    },
    success: {
        label: 'Success',
        paths: (<><circle cx="12" cy="12" r="9" /><path d="m8 12.5 3 3 5-6" /></>),
    },
};

const StatusAlert: React.FC<StatusAlertProps> = ({ variant, className, children }) => {
    const { label, paths } = icons[variant];
    return (
        <div className={`alert alert-${variant} status-alert${className ? ` ${className}` : ''}`} role={variant === 'success' ? 'status' : 'alert'}>
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"
                strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
                {paths}
            </svg>
            <div><span className="visually-hidden">{label}: </span>{children}</div>
        </div>
    );
};

export default StatusAlert;
