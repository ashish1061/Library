import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BookService } from '../../core/services/book.service';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-manage-books',
  templateUrl: './manage-books.html',
  styleUrls: ['./manage-books.css'],
  standalone: false
})
export class ManageBooks implements OnInit {
  bookForm!: FormGroup;

  // Cover image
  coverImagePath: string | null = null;

  // Lookup
  lookupAnum: string | number = '';
  isChecking = false;

  // Raw loaded book data (shown in inventory panel)
  loadedBook: any = null;

  // Active checkouts list to calculate inventory availability
  activeIssues: any[] = [];

  // Submit
  isSaving    = false;
  isUploading = false;
  
  // Bulk Modal
  showBulkModal = false;
  bulkUploadFile: File | null = null;

  constructor(
    private fb: FormBuilder, 
    private bookService: BookService,
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.bookForm = this.fb.group({
      anum:               ['', Validators.required],
      bookName:           ['', Validators.required],
      author:             ['', Validators.required],
      publisher:          ['', Validators.required],
      isbn:               ['', Validators.required],
      edition:            ['', Validators.required],
      numBooks:           [1, [Validators.required, Validators.min(1)]],
      bookClass:          ['', Validators.required],
      category:           ['', Validators.required],
      book_rack:          ['', Validators.required],
      availabilityStatus: ['Available', Validators.required]
    });

    this.loadActiveIssues();
  }

  /** Shortcut for template: f['fieldName'] */
  get f() {
    return this.bookForm.controls;
  }

  /** Look up an existing book by accession number and pre-fill the form */
  checkBook() {
    if (!this.lookupAnum) return;
    this.isChecking = true;
    this.loadActiveIssues(); // Refresh active checkouts from database to ensure fresh count

    this.bookService.getBookByAnum(String(this.lookupAnum)).subscribe({
      next: (res: any) => {
        if (!res) {
          this.toastService.error(`No book found with Accession Number ${this.lookupAnum}.`);
          this.loadedBook = null;
          this.isChecking = false;
          return;
        }
        // Store raw response for inventory panel
        this.loadedBook = res;

        this.bookForm.patchValue({
          anum:               res.anum           || res.Anum        || this.lookupAnum,
          bookName:           res.book_name      || res.Book_name   || res.bookName   || '',
          author:             res.book_author    || res.Book_author || res.author     || '',
          publisher:          res.publisher      || res.Publisher   || '',
          isbn:               res.isbn           || res.ISBN        || '',
          edition:            res.edition        || res.Edition     || '',
          bookClass:          res.book_class     || res.Book_class  || '',
          category:           res.book_category  || res.Book_category || res.category || '',
          book_rack:          res.book_rack      || res.Book_rack || '',
          numBooks:           res.totalCopies    || res.TotalCopies  || 1,
          availabilityStatus: res.available === true ? 'Available'
                              : res.available === false ? 'Not Available'
                              : (res.availabilityStatus || 'Available')
        });
        this.coverImagePath = res.coverImagePath || res.CoverImagePath || null;
        this.toastService.success(`Book "${res.book_name || res.Book_name || res.bookName}" loaded successfully.`);
        this.isChecking = false;
      },
      error: (err) => {
        const msg = err.status === 404
          ? `No book found with Accession Number ${this.lookupAnum}.`
          : 'Failed to load book. Please check the server.';
        this.toastService.error(msg);
        this.isChecking = false;
      }
    });
  }

  /** Handle cover image file selection and upload */
  onFileSelected(event: any) {
    const file: File = event.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      this.toastService.error('Image file must be smaller than 5 MB.');
      return;
    }

    this.isUploading = true;
    if (this.cdr) this.cdr.detectChanges();

    this.bookService.uploadCover(file).subscribe({
      next: (res: any) => {
        try {
          // The API now returns { imagePath: "/images/..." }
          const path = res?.imagePath || res?.coverImagePath || res?.CoverImagePath || '';
          this.coverImagePath = path;
        } catch (err) {
          console.error('Error parsing cover image response', err);
        } finally {
          this.isUploading = false;
          if (this.cdr) this.cdr.detectChanges();
        }
      },
      error: () => {
        this.toastService.error('Failed to upload cover image. Please try again.');
        this.isUploading = false;
        if (this.cdr) this.cdr.detectChanges();
      }
    });
  }

  openBulkModal() {
    this.showBulkModal = true;
    this.bulkUploadFile = null;
  }

  closeBulkModal() {
    this.showBulkModal = false;
    this.bulkUploadFile = null;
  }

  onBulkFileSelected(event: any) {
    this.bulkUploadFile = event.target.files?.[0] || null;
  }

  uploadBulk() {
    if (!this.bulkUploadFile) return;
    this.isUploading = true;
    this.bookService.bulkUpload(this.bulkUploadFile).subscribe({
      next: (res: any) => {
        this.toastService.success(res.message || 'Books uploaded successfully');
        this.isUploading = false;
        this.closeBulkModal();
      },
      error: (err: any) => {
        this.toastService.error(err.error?.message || 'Failed to upload books');
        this.isUploading = false;
      }
    });
  }

  onBulkUpload(event: any) {
    // Legacy fallback if dropped directly
    const file: File = event.target.files?.[0];
    if (!file) return;

    this.bookService.bulkUpload(file).subscribe({
      next: (res: any) => {
        this.toastService.success(res.message || 'Books uploaded successfully');
      },
      error: (err: any) => {
        this.toastService.error(err.error?.message || 'Failed to upload books');
      }
    });
    event.target.value = '';
  }

  // --- Drag and Drop Logic ---
  isDragOver = false;
  isCsvDragOver = false;

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.onFileSelected({ target: { files: files } });
    }
  }

  onCsvDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isCsvDragOver = true;
  }

  onCsvDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isCsvDragOver = false;
  }

  onCsvDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isCsvDragOver = false;
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.onBulkUpload({ target: { files: files } });
    }
  }

  downloadCompleteCsv() {
    this.bookService.getAllBooks().subscribe({
      next: (books: any) => {
        if (!books || books.length === 0) {
          this.toastService.warning('No books available to download.');
          return;
        }

        const headers = ['Anum', 'Book_name', 'Book_author', 'Book_rack', 'Book_class', 'Book_category', 'Available', 'Publisher', 'ISBN', 'Edition', 'TotalCopies'];
        const rows = books.map((b: any) => {
          return [
            b.anum || b.Anum || '',
            b.book_name || b.Book_name || '',
            b.book_author || b.Book_author || '',
            b.book_rack || b.Book_rack || '',
            b.book_class || b.Book_class || '',
            b.book_category || b.Book_category || b.category || '',
            b.available !== false ? 'TRUE' : 'FALSE',
            b.publisher || b.Publisher || '',
            b.isbn || b.ISBN || '',
            b.edition || b.Edition || '',
            b.totalCopies || b.TotalCopies || 1
          ].map(val => `"${val}"`).join(',');
        });

        const csvContent = [headers.join(','), ...rows].join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'complete-books.csv';
        a.style.display = 'none';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.toastService.error('Failed to download complete CSV.');
      }
    });
  }

  /** Clear the whole form and reset state */
  clearForm() {
    this.bookForm.reset({
      numBooks: 1,
      availabilityStatus: 'Available'
    });
    this.coverImagePath = null;
    this.loadedBook = null;
    this.lookupAnum = '';
  }

  /** Submit the form to add / update the book */
  onSubmit() {
    if (this.bookForm.invalid) {
      this.bookForm.markAllAsTouched();
      this.toastService.error('Please fill in all required fields.');
      return;
    }

    this.isSaving = true;

    const v = this.bookForm.value;
    const payload = {
      Anum:               parseInt(v.anum, 10),
      Book_name:          v.bookName,
      Book_author:        v.author,
      Publisher:          v.publisher  || '',
      ISBN:               v.isbn       || '',
      Edition:            v.edition    || '',
      TotalCopies:        v.numBooks   || 1,
      Book_category:      v.category   || '',
      Book_class:         v.bookClass  || '',
      Book_rack:          v.book_rack || '',
      Available:          v.availabilityStatus === 'Available',
      CoverImagePath:     this.coverImagePath  || ''
    };

    this.bookService.addBook(payload).subscribe({
      next: () => {
        this.toastService.success(`Book "${v.bookName}" saved successfully!`);
        this.isSaving = false;
        this.clearForm();
      },
      error: (err) => {
        const msg = err.error?.message
          || (err.status === 409 ? 'A book with this Accession Number already exists.' : 'Failed to save book. Please try again.');
        this.toastService.error(msg);
        this.isSaving = false;
      }
    });
  }

  loadActiveIssues() {
    this.issueService.getActiveIssues().subscribe({
      next: (res: any) => {
        this.activeIssues = res || [];
      },
      error: (err) => {
        console.error('Failed to load active issues:', err);
      }
    });
  }

  get availableCopiesCount(): number {
    if (!this.loadedBook) return 0;
    const anum = this.loadedBook.anum || this.loadedBook.Anum;
    const total = this.loadedBook.totalCopies || this.loadedBook.TotalCopies || 1;
    // Count active issues matching this book's accession number
    const issuedCount = this.activeIssues.filter(i => (i.anum || i.Anum) === anum).length;
    return Math.max(0, total - issuedCount);
  }

  get displayCoverImagePath(): string | null {
    if (!this.coverImagePath) return null;
    if (this.coverImagePath.startsWith('http') || this.coverImagePath.startsWith('data:')) {
      return this.coverImagePath;
    }
    return `${environment.documentBaseUrl}${this.coverImagePath}`;
  }
}
