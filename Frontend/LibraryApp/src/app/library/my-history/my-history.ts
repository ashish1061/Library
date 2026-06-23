import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-my-history',
  standalone: false,
  templateUrl: './my-history.html',
  styleUrl: './my-history.css',
})
export class MyHistory implements OnInit {
  catalogBaseUrl = environment.catalogBaseUrl;
  activeIssues: any[] = [];
  pastIssues: any[] = [];
  myRequests: any[] = [];
  isLoadingActive = false;
  isLoadingPast = false;
  isLoadingRequests = false;
  today = new Date();

  constructor(
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadMyActiveIssues();
    this.loadMyPastIssues();
    this.loadMyRequests();
  }

  parseCustomDate(dateVal: any): Date | null {
    if (!dateVal) return null;
    if (dateVal instanceof Date) return dateVal;
    
    // Try native constructor first
    let d = new Date(dateVal);
    if (!isNaN(d.getTime())) return d;
    
    // Fallback manual parser for dd-MM-yyyy HH:mm:ss format
    if (typeof dateVal === 'string') {
      const parts = dateVal.trim().split(/[- /T:]/);
      if (parts.length >= 3) {
        const day = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10) - 1; // 0-indexed
        const year = parseInt(parts[2].split(' ')[0], 10);
        
        let hour = 0;
        let minute = 0;
        let second = 0;
        if (parts.length >= 6) {
          hour = parseInt(parts[3], 10) || 0;
          minute = parseInt(parts[4], 10) || 0;
          second = parseInt(parts[5], 10) || 0;
        }
        
        const parsed = new Date(year, month, day, hour, minute, second);
        if (!isNaN(parsed.getTime())) return parsed;
      }
    }
    return null;
  }

  loadMyActiveIssues() {
    this.isLoadingActive = true;
    this.issueService.getActiveIssues().subscribe({
      next: (res: any) => {
        let issues = res.map((item: any) => ({
          ...item,
          anum: item.anum ?? item.Anum,
          issueNumber: item.issueNumber ?? item.IssueNumber ?? item.issueID ?? item.IssueID,
          empName: item.empName || item.EmpName,
          bookName: item.bookName || item.BookName,
          issueDate: this.parseCustomDate(item.issueDate || item.IssueDate),
          dueDate: this.parseCustomDate(item.dueDate || item.DueDate),
          coverImagePath: item.coverImagePath || item.CoverImagePath,
          employeeImagePath: item.employeeImagePath || item.EmployeeImagePath,
          itemType: item.itemType || item.ItemType || 'Book'
        }));

        // Filter out dummy/invalid issues (where anum is 0 or bookName is missing)
        issues = issues.filter((i: any) => i.anum !== 0 && i.bookName);

        // Calculate days since issue and check if overdue
        const now = new Date();
        issues = issues.map((i: any) => {
          const issueDate = i.issueDate || now;
          const diffTime = Math.abs(now.getTime() - issueDate.getTime());
          const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
          return { ...i, daysSinceIssue: diffDays };
        });

        // Sort by issue date descending
        issues.sort((a: any, b: any) => {
          const t1 = a.issueDate ? a.issueDate.getTime() : 0;
          const t2 = b.issueDate ? b.issueDate.getTime() : 0;
          return t2 - t1;
        });

        this.activeIssues = issues;
        this.isLoadingActive = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error(err);
        this.isLoadingActive = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadMyPastIssues() {
    this.isLoadingPast = true;
    this.issueService.getIssueHistory().subscribe({
      next: (res: any) => {
        let mappedRes = res.map((item: any) => ({
          ...item,
          anum: item.anum ?? item.Anum,
          issueNumber: item.issueNumber ?? item.IssueNumber,
          empName: item.empName || item.EmpName,
          bookName: item.bookName || item.BookName,
          issueDate: this.parseCustomDate(item.issueDate || item.IssueDate),
          dueDate: this.parseCustomDate(item.dueDate || item.DueDate),
          returnDate: this.parseCustomDate(item.returnDate || item.ReturnDate),
          coverImagePath: item.coverImagePath || item.CoverImagePath,
          employeeImagePath: item.employeeImagePath || item.EmployeeImagePath,
          itemType: item.itemType || item.ItemType || 'Book'
        }));

        // Filter out dummy/invalid issues
        mappedRes = mappedRes.filter((i: any) => i.anum !== 0 && i.bookName);

        // Sort by issue date descending
        mappedRes.sort((a: any, b: any) => {
          const t1 = a.issueDate ? a.issueDate.getTime() : 0;
          const t2 = b.issueDate ? b.issueDate.getTime() : 0;
          return t2 - t1;
        });

        this.pastIssues = mappedRes;
        this.isLoadingPast = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error(err);
        this.isLoadingPast = false;
        this.cdr.markForCheck();
      }
    });
  }

  getOverdueCount(): number {
    return this.activeIssues.filter(item => this.isOverdue(item.dueDate)).length;
  }

  reissueBook(issueNumber: number) {
    this.toastService.confirm('Are you sure you want to request a reissue for this book? This extends the due date by 14 days.').then((confirmReissue) => {
      if (!confirmReissue) return;

      this.issueService.reissueBook(issueNumber).subscribe({
        next: () => {
          this.toastService.success('Book reissued successfully! Due date extended by 14 days.');
          this.loadMyActiveIssues();
        },
        error: (err) => {
          console.error('Reissue failed:', err);
          this.toastService.error('Failed to reissue book. Please try again.');
          this.cdr.markForCheck();
        }
      });
    });
  }

  isOverdue(dueDate: any): boolean {
    if (!dueDate) return false;
    const date = dueDate instanceof Date ? dueDate : this.parseCustomDate(dueDate);
    if (!date) return false;
    return date < this.today;
  }

  loadMyRequests() {
    this.isLoadingRequests = true;
    this.issueService.getAllRequests().subscribe({
      next: (res: any) => {
        let reqs = res.map((item: any) => ({
          ...item,
          requestId: item.requestID ?? item.requestId ?? item.RequestID,
          itemID: item.itemID ?? item.itemId ?? item.ItemID,
          itemName: item.itemName || item.ItemName,
          itemType: item.itemType || item.ItemType || 'Book',
          requestDate: this.parseCustomDate(item.requestDate || item.RequestDate),
          status: item.status || item.Status || 'Pending',
          coverImagePath: item.coverImagePath || item.CoverImagePath
        }));

        // Filter out dummy/invalid requests
        reqs = reqs.filter((r: any) => r.itemID !== 0 && r.itemName);

        // Sort by request date descending
        reqs.sort((a: any, b: any) => {
          const t1 = a.requestDate ? a.requestDate.getTime() : 0;
          const t2 = b.requestDate ? b.requestDate.getTime() : 0;
          return t2 - t1;
        });

        this.myRequests = reqs;
        this.isLoadingRequests = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error(err);
        this.isLoadingRequests = false;
        this.cdr.markForCheck();
      }
    });
  }
}
