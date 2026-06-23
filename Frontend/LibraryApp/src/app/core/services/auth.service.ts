import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.authApiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(credentials: any) {
    return this.http.post(`${this.apiUrl}/login`, credentials);
  }

  getEmpId(): string | null {
    const token = localStorage.getItem('accessToken');
    if (!token) return null;
    try {
      const decoded: any = jwtDecode(token);
      return decoded.empId || decoded['empId'] || null;
    } catch (e) {
      return null;
    }
  }

  isAdmin(): boolean {
    const token = localStorage.getItem('accessToken');
    if (!token) return false;
    try {
      const decoded: any = jwtDecode(token);
      const role = decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      return role === 'Admin';
    } catch {
      return false;
    }
  }

  register(employee: any) {
    return this.http.post(`${this.apiUrl}/register`, employee);
  }

  changePassword(data: any) {
    return this.http.post(`${this.apiUrl}/change-password`, data);
  }

  forceChangePassword(data: any) {
    return this.http.post(`${this.apiUrl}/force-change-password`, data);
  }

  adminChangePassword(data: any) {
    return this.http.post(`${this.apiUrl}/admin-change-password`, data);
  }

  verifyOtp(email: string, otp: string) {
    return this.http.post(`${this.apiUrl}/verify-otp`, { email, otp });
  }

  refreshToken(accessToken: string, refreshToken: string) {
    return this.http.post<any>(`${this.apiUrl}/refresh-token`, { accessToken, refreshToken });
  }
}
