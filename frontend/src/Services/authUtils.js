export function parseJwt(token) {
  if (!token) return null;
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const payload = parts[1];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decodeURIComponent(escape(decoded)));
  } catch (e) {
    console.error('Failed to parse JWT', e);
    return null;
  }
}

export function getUserRoles() {
  const token = localStorage.getItem('token');
  const payload = parseJwt(token);
  if (!payload) return [];

  // Common claim names used by different backends
  const possible = [
    payload.roles,
    payload.role,
    payload.Roles,
    payload.Role,
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
    payload['roles']
  ].filter(Boolean);

  if (possible.length === 0) return [];

  const first = possible[0];
  if (Array.isArray(first)) return first.map(String);
  if (typeof first === 'string') return first.split(',').map((s) => s.trim());
  return [];
}

export function getCurrentUser() {
  const token = localStorage.getItem('token');
  const payload = parseJwt(token);
  if (!payload) return null;

  return {
    username:
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
      payload.unique_name ||
      payload.preferred_username ||
      payload.name ||
      null,
    fullName: payload.fullName || null
  };
}

export function hasAnyRole(allowedRoles = []) {
  if (!allowedRoles || allowedRoles.length === 0) return true;
  const userRoles = getUserRoles();
  return allowedRoles.some((r) => userRoles.includes(r));
}
