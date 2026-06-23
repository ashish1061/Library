import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = `${environment.operationsApiUrl}/reports`;

  constructor(private http: HttpClient) {}

  downloadBooksReport() {
    return this.http.get(`${this.apiUrl}/books`, { responseType: 'blob' });
  }

  downloadActiveIssuesReport() {
    return this.http.get(`${this.apiUrl}/active-issues`, { responseType: 'blob' });
  }

  downloadIssueHistoryReport(startDate?: string, endDate?: string) {
    let url = `${this.apiUrl}/issue-history`;
    if (startDate && endDate) {
      url += `?startDate=${startDate}&endDate=${endDate}`;
    }
    return this.http.get(url, { responseType: 'blob' });
  }
}
