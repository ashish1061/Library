import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { BookService } from '../../core/services/book.service';
import { MagazineService } from '../../core/services/magazine.service';
import { IssueService } from '../../core/services/issue.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-request-issue',
  standalone: false,
  templateUrl: './request-issue.html',
  styleUrl: './request-issue.css',
})
export class RequestIssue implements OnInit {
  documentBaseUrl = environment.documentBaseUrl;
  
  books: any[] = [];
  magazines: any[] = [];
  allItems: any[] = [];
  filteredItems: any[] = [];
  
  showConfirmPopup: boolean = false;
  selectedItemForRequest: any = null;

  keyword: string = '';
  itemTypeFilter: string = 'All'; // 'All', 'Book', 'Magazine'
  
  isLoading = false;

  // Pagination
  currentPage: number = 1;
  pageSize: number = 20;

  get pagedItems() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredItems.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    return Math.ceil(this.filteredItems.length / this.pageSize) || 1;
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
    private magazineService: MagazineService,
    private issueService: IssueService,
    private authService: AuthService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCatalog();
  }

  loadCatalog() {
    this.isLoading = true;
    this.cdr.detectChanges();

    let booksLoaded = false;
    let magazinesLoaded = false;

    // Fetch Books
    this.bookService.searchBooks('', '').subscribe({
      next: (res: any) => {
        this.books = (res || []).map((b: any) => ({
          ...b,
          ItemType: 'Book',
          ItemID: b.Anum || b.anum,
          ItemName: b.Book_name || b.BookName || b.book_name,
          Author: b.Book_author || b.BookAuthor || b.book_author,
          Category: b.Book_category || b.BookCategory || b.book_category || b.category,
          IsAvailable: b.Available || b.available
        }));
        booksLoaded = true;
        this.checkAllLoaded(booksLoaded, magazinesLoaded);
      },
      error: () => { booksLoaded = true; this.checkAllLoaded(booksLoaded, magazinesLoaded); }
    });

    // Fetch Magazines
    this.magazineService.getAllMagazines().subscribe({
      next: (res: any) => {
        this.magazines = (res || []).map((m: any) => ({
          ...m,
          ItemType: 'Magazine',
          ItemID: m.MagazineId || m.magazineId,
          ItemName: m.Title || m.title,
          Author: m.Publisher || m.publisher,
          Category: m.Category || m.category,
          IsAvailable: (m.AvailableCopies || m.availableCopies) > 0,
          CoverImagePath: m.CoverImagePath || m.coverImagePath
        }));
        magazinesLoaded = true;
        this.checkAllLoaded(booksLoaded, magazinesLoaded);
      },
      error: () => { magazinesLoaded = true; this.checkAllLoaded(booksLoaded, magazinesLoaded); }
    });
  }

  checkAllLoaded(b: boolean, m: boolean) {
    if (b && m) {
      this.allItems = [...this.books, ...this.magazines];
      this.applyFilters();
      this.isLoading = false;
      this.cdr.detectChanges();
    }
  }

  onSearchChange() {
    this.applyFilters();
  }

  applyFilters() {
    let temp = this.allItems;
    if (this.itemTypeFilter !== 'All') {
      temp = temp.filter(x => x.ItemType === this.itemTypeFilter);
    }
    if (this.keyword.trim() !== '') {
      const k = this.keyword.toLowerCase();
      temp = temp.filter(x => 
        (x.ItemName && x.ItemName.toLowerCase().includes(k)) ||
        (x.Author && x.Author.toLowerCase().includes(k))
      );
    }
    this.filteredItems = temp;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  requestIssue(item: any) {
    console.log("requestIssue clicked for item:", item);
    const empId = this.authService.getEmpId();
    if (!empId) {
      this.toastService.error("You must be logged in to request items.");
      return;
    }
    
    if (!item.IsAvailable) {
      this.toastService.error("This item is currently unavailable.");
      return;
    }

    console.log("Opening confirm popup...");
    this.selectedItemForRequest = item;
    this.showConfirmPopup = true;
    this.cdr.detectChanges();
  }

  confirmRequest() {
    if (!this.selectedItemForRequest) return;
    const empId = this.authService.getEmpId();
    if (!empId) {
      this.toastService.error("You must be logged in to request items.");
      return;
    }
    const item = this.selectedItemForRequest;

    const payload = {
      empID: empId,
      itemType: item.ItemType,
      itemID: item.ItemID,
      itemName: item.ItemName
    };

    this.issueService.createRequest(payload).subscribe({
      next: () => {
        this.toastService.success(`Request submitted for ${item.ItemName}!`);
        this.showConfirmPopup = false;
        this.selectedItemForRequest = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to submit request.');
        this.showConfirmPopup = false;
        this.selectedItemForRequest = null;
        this.cdr.detectChanges();
      }
    });
  }

  cancelRequest() {
    this.showConfirmPopup = false;
    this.selectedItemForRequest = null;
    this.cdr.detectChanges();
  }

  getPlaceholderGradient(item: any): string {
    const num = item.ItemID || 0;
    const gradients = [
      'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
      'linear-gradient(135deg, #10b981 0%, #047857 100%)',
      'linear-gradient(135deg, #f59e0b 0%, #b45309 100%)',
      'linear-gradient(135deg, #ec4899 0%, #be185d 100%)',
      'linear-gradient(135deg, #8b5cf6 0%, #5b21b6 100%)',
      'linear-gradient(135deg, #06b6d4 0%, #0e7490 100%)'
    ];
    return gradients[num % gradients.length];
  }
}
