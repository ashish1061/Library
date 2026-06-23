import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ExecutionPlanService {
  private apiUrl = `${environment.catalogApiUrl}/executionplan`;

  constructor(private http: HttpClient) {}

  uploadPlan(formData: FormData) {
    return this.http.post(`${this.apiUrl}/upload`, formData);
  }
}
