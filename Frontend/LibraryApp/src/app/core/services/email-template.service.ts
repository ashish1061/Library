import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface EmailTemplate {
  templateId: number;
  purpose: string;
  subject: string;
  body: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmailTemplateService {
  private apiUrl = `${environment.operationsApiUrl}/emailtemplates`;

  constructor(private http: HttpClient) {}

  getAllTemplates() {
    return this.http.get<EmailTemplate[]>(this.apiUrl);
  }

  addTemplate(template: EmailTemplate) {
    return this.http.post<any>(this.apiUrl, template);
  }

  updateTemplate(id: number, template: EmailTemplate) {
    return this.http.put<any>(`${this.apiUrl}/${id}`, template);
  }
}
