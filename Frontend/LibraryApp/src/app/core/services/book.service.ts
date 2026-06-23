import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.catalogApiUrl}/books`;

  constructor(private http: HttpClient) {}

  searchBooks(category: string, keyword: string) {
    let url = `${this.apiUrl}/search?`;
    if (category) url += `category=${encodeURIComponent(category)}&`;
    if (keyword) url += `keyword=${encodeURIComponent(keyword)}`;
    return this.http.get(url);
  }

  getCategories() {
    return this.http.get<string[]>(`${this.apiUrl}/categories`);
  }

  getAllBooks() {
    return this.http.get(`${this.apiUrl}`);
  }

  getBookByAnum(anum: string) {
    return this.http.get(`${this.apiUrl}/${anum}`);
  }

  addBook(book: any) {
    return this.http.post(this.apiUrl, book);
  }

  uploadCover(file: File) {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ imagePath: string }>(`${environment.documentApiUrl}/media/upload`, formData);
  }

  reserveBook(anum: number, empId: string) {
    return this.http.post(`${environment.catalogApiUrl}/reservations`, { Anum: anum, EmpID: empId });
  }

  bulkUpload(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/bulk-upload`, formData);
  }
}
