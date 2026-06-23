import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardSummary {
  totalBooks: number;
  activeIssues: number;
  registeredMembers: number;
  totalIssues?: number;
}

export interface CategoryDistribution {
  category: string;
  count: number;
}

export interface IssueTrend {
  month: string;
  issueCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private apiUrl = `${environment.operationsApiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/summary`);
  }

  getBooksByCategory(): Observable<CategoryDistribution[]> {
    return this.http.get<CategoryDistribution[]>(`${this.apiUrl}/books-by-category`);
  }

  getIssueTrends(): Observable<IssueTrend[]> {
    return this.http.get<IssueTrend[]>(`${this.apiUrl}/issue-trends`);
  }
}
