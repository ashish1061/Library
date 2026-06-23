import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class IssueService {
  private apiUrl = `${environment.operationsApiUrl}/issues`;

  constructor(private http: HttpClient) {}

  getActiveIssues() {
    return this.http.get(`${this.apiUrl}/active`); // Note: you might need to adjust this if using the new Library.API which returns all via GET /api/issues
  }

  getIssuesByAnum(anum: string | number) {
    return this.http.get(`${this.apiUrl}/book/${anum}`);
  }

  issueBook(payload: any) {
    return this.http.post(`${this.apiUrl}/issue`, payload);
  }

  getIssueHistory() {
    return this.http.get(`${this.apiUrl}/history`);
  }

  exportIssues(startDate?: string, endDate?: string) {
    let url = `${this.apiUrl}/export`;
    if (startDate && endDate) {
      url += `?startDate=${startDate}&endDate=${endDate}`;
    }
    return this.http.get(url, { responseType: 'blob' });
  }

  returnBook(issueNumber: number, returnDate: string) {
    return this.http.post(`${this.apiUrl}/return`, { issueNumber, returnDate });
  }

  reissueBook(issueNumber: number) {
    return this.http.post(`${this.apiUrl}/reissue/${issueNumber}`, {});
  }

  sendReminders(issueNumbers: number[]) {
    return this.http.post(`${this.apiUrl}/remind`, issueNumbers);
  }

  // Issue Requests
  createRequest(payload: { empID: string, itemType: string, itemID: number, itemName: string }) {
    return this.http.post(`${this.apiUrl}/requests`, payload);
  }

  getPendingRequests() {
    return this.http.get<any[]>(`${this.apiUrl}/requests/pending`);
  }

  getAllRequests() {
    return this.http.get<any[]>(`${this.apiUrl}/requests`);
  }

  approveRequests(requestIds: number[]) {
    return this.http.post(`${this.apiUrl}/requests/approve`, requestIds);
  }

  rejectRequests(requestIds: number[]) {
    return this.http.post(`${this.apiUrl}/requests/reject`, requestIds);
  }
}
