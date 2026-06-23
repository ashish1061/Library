import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { BookService } from '../../core/services/book.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-book-search',
  standalone: false,
  templateUrl: './book-search.html',
  styleUrl: './book-search.css',
})
export class BookSearch implements OnInit, OnDestroy {
  catalogBaseUrl = environment.catalogBaseUrl;
  documentBaseUrl = environment.documentBaseUrl;
  books: any[] = [];
  keyword: string = '';
  category: string = '';
  availableCategories: string[] = [];
  isLoading = false;

  private searchSubject = new Subject<void>();
  private searchSubscription!: Subscription;

  // Pagination
  currentPage: number = 1;
  pageSize: number = 20;

  allBooks: any[] = [];
  filteredBooks: any[] = [];

  get pagedBooks() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredBooks.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    return Math.ceil(this.filteredBooks.length / this.pageSize) || 1;
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage() {
    if (this.currentPage > 1) this.currentPage--;
  }

  onPageSizeChange() {
    this.currentPage = 1;
  }

  constructor(
    private bookService: BookService, 
    private authService: AuthService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCategories();
    this.loadAllBooks();
    
    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300)
    ).subscribe(() => {
      this.filterBooksLocally();
    });
  }

  ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  onSearchChange() {
    this.searchSubject.next();
  }

  loadCategories() {
    this.bookService.getCategories().subscribe({
      next: (res) => {
        this.availableCategories = res;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load categories', err)
    });
  }

  loadAllBooks() {
    this.isLoading = true;
    this.cdr.detectChanges();

    this.bookService.searchBooks('', '').subscribe({
      next: (res: any) => {
        this.allBooks = res || [];
        this.filteredBooks = this.allBooks;
        this.currentPage = 1;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  filterBooksLocally() {
    let temp = this.allBooks;
    
    if (this.category) {
      temp = temp.filter(b => (b.Book_category || b.book_category || b.category) === this.category);
    }
    
    if (this.keyword.trim() !== '') {
      const k = this.keyword.toLowerCase();
      temp = temp.filter(b => 
        (b.Book_name && b.Book_name.toLowerCase().includes(k)) ||
        (b.book_name && b.book_name.toLowerCase().includes(k)) ||
        (b.BookName && b.BookName.toLowerCase().includes(k)) ||
        (b.Book_author && b.Book_author.toLowerCase().includes(k)) ||
        (b.book_author && b.book_author.toLowerCase().includes(k)) ||
        (b.BookAuthor && b.BookAuthor.toLowerCase().includes(k)) ||
        (b.ISBN && b.ISBN.toLowerCase().includes(k)) ||
        (b.isbn && b.isbn.toLowerCase().includes(k))
      );
    }
    
    this.filteredBooks = temp;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  searchBooks() {
    this.filterBooksLocally();
  }

  reserveBook(anum: number) {
    const empId = this.authService.getEmpId();
    if (!empId) {
      this.toastService.error("You must be logged in to reserve books.");
      return;
    }
    this.bookService.reserveBook(anum, empId).subscribe({
      next: () => this.toastService.success('Book reserved successfully! You will be notified when it is available.'),
      error: () => this.toastService.error('Failed to reserve book. You may have already reserved it.')
    });
  }

  getPlaceholderGradient(book: any): string {
    const anum = book.Anum || book.anum || 0;
    const gradients = [
      'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
      'linear-gradient(135deg, #10b981 0%, #047857 100%)',
      'linear-gradient(135deg, #f59e0b 0%, #b45309 100%)',
      'linear-gradient(135deg, #ec4899 0%, #be185d 100%)',
      'linear-gradient(135deg, #8b5cf6 0%, #5b21b6 100%)',
      'linear-gradient(135deg, #06b6d4 0%, #0e7490 100%)',
      'linear-gradient(135deg, #f43f5e 0%, #be123c 100%)'
    ];
    return gradients[anum % gradients.length];
  }
}
