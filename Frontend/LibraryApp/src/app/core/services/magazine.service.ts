import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MagazineService {
  private apiUrl = `${environment.catalogApiUrl}/magazines`;

  constructor(private http: HttpClient) {}

  getAllMagazines() {
    return this.http.get(this.apiUrl);
  }

  getMagazineById(id: number) {
    return this.http.get(`${this.apiUrl}/${id}`);
  }

  addMagazine(magazine: any) {
    return this.http.post(this.apiUrl, magazine);
  }

  updateMagazine(id: number, magazine: any) {
    return this.http.put(`${this.apiUrl}/${id}`, magazine);
  }

  deleteMagazine(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  uploadCover(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ imagePath: string }>(`${environment.documentApiUrl}/media/upload`, formData);
  }

  bulkUpload(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post(`${this.apiUrl}/bulk-upload`, formData);
  }
}
