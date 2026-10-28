import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

function ensureUser() {
  const auth = inject(AuthService);
  if (auth.user()) return of(auth.user());
  if (!auth.token()) return of(null);
  return auth.me().pipe(catchError(() => {
    auth.logout();
    return of(null);
  }));
}

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  return ensureUser().pipe(
    map((u) => (u ? true : router.createUrlTree(['/login'])))
  );
};

export const contentAuthorGuard: CanActivateFn = () => {
  const router = inject(Router);
  return ensureUser().pipe(
    map((u) => {
      if (!u) return router.createUrlTree(['/login']);
      const roles = u.roles ?? [];
      if (roles.includes('Admin') || roles.includes('Teacher')) return true;
      if (roles.includes('Student')) return router.createUrlTree(['/practice']);
      return router.createUrlTree(['/login']);
    })
  );
};

/** Student practice; Teacher/Admin may preview. */
export const practiceGuard: CanActivateFn = () => {
  const router = inject(Router);
  return ensureUser().pipe(
    map((u) => {
      if (!u) return router.createUrlTree(['/login']);
      const roles = u.roles ?? [];
      if (roles.includes('Student') || roles.includes('Teacher') || roles.includes('Admin')) return true;
      return router.createUrlTree(['/login']);
    })
  );
};
