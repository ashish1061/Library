import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Store } from '@ngxs/store';
import { AuthState } from '../../store/auth.state';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(private store: Store, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const token = this.store.selectSnapshot(AuthState.token);
    
    if (!token) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    try {
      const decodedToken: any = jwtDecode(token);
      const userRole = decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decodedToken.role;
      
      const expectedRole = route.data['expectedRole'];
      
      if (expectedRole && expectedRole !== userRole) {
        // Redirect somewhere, e.g., unauthorized or home
        this.router.navigate(['/library/dashboard']);
        return false;
      }
      
      return true;
    } catch(e) {
      this.router.navigate(['/auth/login']);
      return false;
    }
  }
}
