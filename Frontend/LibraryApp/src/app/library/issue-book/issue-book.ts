import { Component, ChangeDetectorRef, OnInit, OnDestroy } from '@angular/core';
import { BookService } from '../../core/services/book.service';
import { MagazineService } from '../../core/services/magazine.service';
import { EmployeeService } from '../../core/services/employee.service';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-issue-book',
  standalone: false,
  templateUrl: './issue-book.html',
  styleUrl: './issue-book.css',
})
export class IssueBook implements OnInit, OnDestroy {
  anum: string = '';
  empId: string = '';
  issueDate: string = new Date().toISOString().split('T')[0];

  bookName: string = '';
  empName: string = '';
  itemType: 'Book' | 'Magazine' = 'Book';

  searchBooksTerm: string = '';
  searchResultsBooks: any[] = [];
  isSearchingBooks = false;
  showBookDropdown = false;

  searchEmpTerm: string = '';
  searchResultsEmp: any[] = [];
  isSearchingEmp = false;
  showEmpDropdown = false;

  bookSearchSubject = new Subject<string>();
  empSearchSubject = new Subject<string>();
  private subs: Subscription = new Subscription();

  isIssuing = false;
  bookTotalCopies: number = 1;
  bookActiveIssues: any[] = [];
  bookAvailableCopies: number = 0;

  constructor(
    private bookService: BookService,
    private magazineService: MagazineService,
    private employeeService: EmployeeService,
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.subs.add(
      this.bookSearchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(term => {
        this.executeBookSearch(term);
      })
    );
    this.subs.add(
      this.empSearchSubject.pipe(debounceTime(300), distinctUntilChanged()).subscribe(term => {
        this.executeEmpSearch(term);
      })
    );
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  onBookSearchChange(term: string) {
    this.searchBooksTerm = term;
    if (term.length > 2) {
      this.showBookDropdown = true;
      this.bookSearchSubject.next(term);
    } else {
      this.showBookDropdown = false;
      this.searchResultsBooks = [];
    }
  }

  onEmpSearchChange(term: string) {
    this.searchEmpTerm = term;
    if (term.length > 2) {
      this.showEmpDropdown = true;
      this.empSearchSubject.next(term);
    } else {
      this.showEmpDropdown = false;
      this.searchResultsEmp = [];
    }
  }

  executeBookSearch(term: string) {
    this.isSearchingBooks = true;
    
    if (this.itemType === 'Book') {
      this.bookService.searchBooks('', term).subscribe({
        next: (res: any) => {
          this.searchResultsBooks = Array.isArray(res) ? res : [];
          this.isSearchingBooks = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.searchResultsBooks = [];
          this.isSearchingBooks = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      // Magazine search (client-side filter as there's no search API yet)
      this.magazineService.getAllMagazines().subscribe({
        next: (res: any) => {
          const allMags = Array.isArray(res) ? res : [];
          const lowerTerm = term.toLowerCase();
          this.searchResultsBooks = allMags.filter(m => 
            (m.title && m.title.toLowerCase().includes(lowerTerm)) || 
            (m.publisher && m.publisher.toLowerCase().includes(lowerTerm)) ||
            (m.magazineId && m.magazineId.toString().includes(lowerTerm))
          );
          this.isSearchingBooks = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.searchResultsBooks = [];
          this.isSearchingBooks = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  executeEmpSearch(term: string) {
    this.isSearchingEmp = true;
    this.employeeService.getAllEmployees().subscribe({
      next: (res: any) => {
        const allEmps = Array.isArray(res) ? res : [];
        const lowerTerm = term.toLowerCase();
        this.searchResultsEmp = allEmps.filter(e => {
          const email = e.emailid || e.Emailid || e.email || '';
          return email.toLowerCase().includes(lowerTerm);
        });
        this.isSearchingEmp = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.searchResultsEmp = [];
        this.isSearchingEmp = false;
        this.cdr.detectChanges();
      }
    });
  }

  selectBook(book: any) {
    if (this.itemType === 'Book') {
      this.anum = book.anum || book.Anum;
      this.bookName = book.book_name || book.Book_name || '';
      this.searchBooksTerm = `${this.bookName} (${this.anum})`;
      this.bookTotalCopies = book.availableCopies ?? book.AvailableCopies ?? book.totalCopies ?? book.TotalCopies ?? 1;
    } else {
      this.anum = book.magazineId || book.MagazineId;
      this.bookName = book.title || book.Title || '';
      this.searchBooksTerm = `${this.bookName} (${this.anum})`;
      this.bookTotalCopies = book.availableCopies ?? book.AvailableCopies ?? book.totalCopies ?? book.TotalCopies ?? 1;
    }
    this.showBookDropdown = false;
    this.checkBookAvailability();
  }

  employeeActiveIssues: any[] = [];
  isFetchingEmpIssues = false;

  selectEmployee(emp: any) {
    this.empId = emp.emailid || emp.email || emp.Emailid || '';
    this.empName = emp.empName || emp.EmpName || '';
    this.searchEmpTerm = `${this.empName} (${this.empId})`;
    this.showEmpDropdown = false;
    this.checkEmployeeActiveIssues(emp.empID || emp.EmpID);
  }

  checkEmployeeActiveIssues(actualEmpId: string) {
    this.isFetchingEmpIssues = true;
    this.issueService.getActiveIssues().subscribe({
      next: (res: any) => {
        const allIssues = Array.isArray(res) ? res : [];
        this.employeeActiveIssues = allIssues.filter(i => 
          (i.empID === actualEmpId || i.EmpID === actualEmpId)
        );
        this.isFetchingEmpIssues = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.employeeActiveIssues = [];
        this.isFetchingEmpIssues = false;
        this.cdr.detectChanges();
      }
    });
  }

  checkBookAvailability() {
    this.issueService.getIssuesByAnum(this.anum).subscribe({
      next: (issuesRes: any) => {
        this.bookActiveIssues = issuesRes || [];
        this.bookAvailableCopies = this.bookTotalCopies;
        this.cdr.detectChanges();
      },
      error: () => {
        this.bookActiveIssues = [];
        this.bookAvailableCopies = this.bookTotalCopies;
        this.cdr.detectChanges();
      }
    });
  }

  issueBook() {
    if (!this.anum || !this.empId || !this.bookName || !this.empName || !this.issueDate) {
      this.toastService.error('Please select both Book and Employee from the search dropdowns before issuing.');
      return;
    }

    if (this.bookAvailableCopies <= 0) {
      this.toastService.error('This book currently has no available copies to issue.');
      return;
    }

    this.isIssuing = true;

    const payload = {
      Anum: parseInt(this.anum, 10),
      BookName: this.bookName,
      EmpID: this.empId.trim(),
      EmpName: this.empName,
      IssueDate: this.issueDate + ' 12:00:00',
      ItemType: this.itemType
    };

    this.issueService.issueBook(payload).subscribe({
      next: () => {
        this.toastService.success(`Book "${this.bookName}" issued successfully to ${this.empName}!`);
        this.isIssuing = false;
        this.resetForm();
        this.cdr.detectChanges();
      },
      error: (err) => {
        if (err.status === 409 || (err.error && err.error.message?.toLowerCase().includes('already'))) {
          this.toastService.error('This book is already issued and not yet returned.');
        } else {
          this.toastService.error(err.error?.message || 'Failed to issue book. Please try again.');
        }
        this.isIssuing = false;
        this.cdr.detectChanges();
      }
    });
  }

  resetForm() {
    this.anum = '';
    this.empId = '';
    this.bookName = '';
    this.empName = '';
    this.searchBooksTerm = '';
    this.searchEmpTerm = '';
    this.issueDate = new Date().toISOString().split('T')[0];
    this.bookActiveIssues = [];
    this.employeeActiveIssues = [];
    this.bookTotalCopies = 1;
    this.bookAvailableCopies = 0;
  }
}
