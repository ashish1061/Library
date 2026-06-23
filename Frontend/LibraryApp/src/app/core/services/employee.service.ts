import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private apiUrl = `${environment.authApiUrl}/employees`;
  private syncUrl = `${environment.syncUrl}/sync`;

  constructor(private http: HttpClient) {}

  getAllEmployees() {
    return this.http.get(this.apiUrl);
  }

  addEmployee(employee: any) {
    return this.http.post(this.apiUrl, employee);
  }

  getEmployeeById(empId: string) {
    return this.http.get(`${this.apiUrl}/${empId}`);
  }

  bulkUpdateEmployees(employees: any[]) {
    return this.http.put(`${this.apiUrl}/bulk`, employees);
  }

  syncDarwinbox() {
    return this.http.post(`${this.syncUrl}/darwinbox`, {});
  }

  getProfilePicFromDarwinbox(empId: string) {
    return this.http.get<{ data: string }>(`${this.syncUrl}/darwinbox/profile-pic/${empId}`);
  }

  bulkUpload(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/bulk-upload`, formData);
  }

  uploadAvatar(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ imagePath: string }>(`${environment.documentApiUrl}/media/upload`, formData);
  }

  toggleMfa(empId: string, isMfaEnabled: boolean) {
    return this.http.put(`${this.apiUrl}/${empId}/toggle-mfa?isMfaEnabled=${isMfaEnabled}`, {});
  }

  toggleAdmin(empId: string, isAdmin: boolean) {
    return this.http.put(`${this.apiUrl}/${empId}/toggle-admin?isAdmin=${isAdmin}`, {});
  }

  bulkToggleMfa(empIds: string[], isMfaEnabled: boolean) {
    return this.http.put(`${this.apiUrl}/bulk-toggle-mfa`, { empIds, isMfaEnabled });
  }
}
