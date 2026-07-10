import React from 'react';
import { hasAnyRole } from '../../Services/authUtils';

const RequireRole = ({ roles = [], children, fallback = null }) => {
  if (hasAnyRole(roles)) return <>{children}</>;
  return <>{fallback}</>;
};

export default RequireRole;
