import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-return-book',
  standalone: false,
  templateUrl: './return-book.html',
  styleUrl: './return-book.css',
})
export class ReturnBook implements OnInit {
  catalogBaseUrl = environment.catalogBaseUrl;
  activeIssues: any[] = [];
  isLoading = false;
  today = new Date();
  highlightedIssueNumber: number | null = null;
  
  // Search and Pagination properties
  searchText: string = '';
  currentPage: number = 1;
  pageSize: number = 20; // Default to 20 records per page as requested

  constructor(
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['issueNumber']) {
        this.highlightedIssueNumber = +params['issueNumber'];
        // We removed setting searchText so we don't hide other rows
      }
    });
    this.loadActiveIssues();
  }

  loadActiveIssues() {
    this.isLoading = true;
    this.issueService.getActiveIssues().subscribe({
      next: (res: any) => {
        let issues = res.map((item: any) => ({
          ...item,
          issueNumber: item.issueNumber ?? item.IssueNumber ?? item.issueID ?? item.IssueID,
          empName: item.empName || item.EmpName,
          bookName: item.bookName || item.BookName,
          issueDate: item.issueDate || item.IssueDate,
          dueDate: item.dueDate || item.DueDate,
          coverImagePath: item.coverImagePath || item.CoverImagePath,
          employeeImagePath: item.employeeImagePath || item.EmployeeImagePath
        }));
        
        // Calculate days since issue
        const now = new Date();
        issues = issues.map((i: any) => {
          let issueDate = new Date(i.issueDate);
          if (isNaN(issueDate.getTime()) && typeof i.issueDate === 'string') {
            // Try parsing dd-MM-yyyy format
            const parts = i.issueDate.split(/[- /]/);
            if (parts.length >= 3) {
              const d = parts[0];
              const m = parts[1];
              const y = parts[2].split(' ')[0]; // ignore time part if exists
              issueDate = new Date(`${y}-${m}-${d}`);
            }
          }
          const diffTime = Math.abs(now.getTime() - issueDate.getTime());
          const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));
          return { ...i, parsedDate: issueDate, daysSinceIssue: diffDays, selected: false };
        });

        // Sort issues by parsedDate descending (latest first)
        issues.sort((a: any, b: any) => b.parsedDate.getTime() - a.parsedDate.getTime());

        // If there's a highlighted issue, bring it to the very top after sorting
        if (this.highlightedIssueNumber) {
          const highlightedIndex = issues.findIndex((i: any) => i.issueNumber == this.highlightedIssueNumber);
          if (highlightedIndex > -1) {
            const highlightedItem = issues.splice(highlightedIndex, 1)[0];
            issues.unshift(highlightedItem);
          }
        }

        this.activeIssues = issues;
        this.isLoading = false;
        this.currentPage = 1; // reset to first page on load
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  // Derived properties for searching and paging
  get filteredIssues() {
    if (!this.searchText.trim()) {
      return this.activeIssues;
    }
    const lowerSearch = this.searchText.toLowerCase();
    return this.activeIssues.filter(issue => {
      const empId = (issue.empId || issue.empID || issue.EmpID || '').toString().toLowerCase();
      const empName = (issue.empName || issue.EmpName || '').toString().toLowerCase();
      const bookName = (issue.bookName || issue.BookName || '').toString().toLowerCase();
      const anum = (issue.anum || issue.Anum || '').toString().toLowerCase();
      const issueNum = (issue.issueNumber || issue.IssueNumber || '').toString().toLowerCase();

      return empId.includes(lowerSearch) ||
             empName.includes(lowerSearch) ||
             bookName.includes(lowerSearch) ||
             anum.includes(lowerSearch) ||
             issueNum.includes(lowerSearch);
    });
  }

  onSearchChange() {
    this.currentPage = 1;
    this.cdr.markForCheck();
  }

  onPageSizeChange() {
    this.currentPage = 1;
    this.cdr.markForCheck();
  }

  get paginatedIssues() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredIssues.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    return Math.ceil(this.filteredIssues.length / this.pageSize) || 1;
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.cdr.markForCheck();
    }
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.cdr.markForCheck();
    }
  }



  returnBook(issueNumber: number) {
    this.toastService.confirm('Are you sure you want to process the return for this book?').then((confirmReturn) => {
      if (!confirmReturn) {
        this.toastService.warning('Return action cancelled.');
        return;
      }

      const today = new Date().toISOString().split('T')[0];
      this.issueService.returnBook(issueNumber, today).subscribe({
        next: () => {
          this.toastService.success('Book returned successfully!');
          this.loadActiveIssues();
        },
        error: (err) => {
          console.error('Return failed:', err);
          this.toastService.error('Failed to return book. Please try again.');
          this.cdr.markForCheck();
        }
      });
    });
  }

  isOverdue(dueDateStr: string): boolean {
    if (!dueDateStr) return false;
    const dueDate = new Date(dueDateStr);
    return dueDate < this.today;
  }

  // Checkbox logic
  get anySelected(): boolean {
    return this.filteredIssues.some(i => i.selected);
  }

  get allSelected(): boolean {
    return this.filteredIssues.length > 0 && this.filteredIssues.every(i => i.selected);
  }

  toggleAll(event: any) {
    const checked = event.target.checked;
    this.filteredIssues.forEach(i => i.selected = checked);
  }

  // Send Reminder Logic
  isSendingReminder = false;
  sendReminder(singleIssue?: any) {
    const issuesToRemind = singleIssue ? [singleIssue] : this.filteredIssues.filter(i => i.selected);
    if (issuesToRemind.length === 0) return;

    this.isSendingReminder = true;
    
    // Call the remind API
    const issueNumbers = issuesToRemind.map(i => i.issueNumber);
    
    this.issueService.sendReminders(issueNumbers).subscribe({
      next: () => {
        this.toastService.success(`Reminders sent successfully to ${issueNumbers.length} employees.`);
        this.isSendingReminder = false;
        // Uncheck all after sending
        this.activeIssues.forEach(i => i.selected = false);
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.toastService.error('Failed to send reminders.');
        console.error(err);
        this.isSendingReminder = false;
        this.cdr.detectChanges();
      }
    });
  }

  reissueBook(issueNumber: number) {
    this.toastService.confirm('Are you sure you want to reissue this book and extend the due date by 14 days?').then((confirmReissue) => {
      if (!confirmReissue) {
        this.toastService.warning('Reissue action cancelled.');
        return;
      }

      this.issueService.reissueBook(issueNumber).subscribe({
        next: () => {
          this.toastService.success('Book reissued successfully! Due date extended by 14 days.');
          this.loadActiveIssues();
        },
        error: (err) => {
          console.error('Reissue failed:', err);
          this.toastService.error('Failed to reissue book. Please try again.');
          this.cdr.markForCheck();
        }
      });
    });
  }

  exportExcel() {
    if (!this.filteredIssues || this.filteredIssues.length === 0) {
      this.toastService.info('No active issues to export.');
      return;
    }
    
    let csvContent = 'Issue Number,Book Title,Issued To,Issue Date,Due Date,Days Since Issue\n';
    
    this.filteredIssues.forEach(item => {
      const issueNum = item.issueNumber;
      const title = item.bookName ? item.bookName.replace(/,/g, '') : '';
      const name = item.empName ? item.empName.replace(/,/g, '') : '';
      const iDate = item.issueDate ? String(item.issueDate).replace(/,/g, '') : '';
      const dDate = item.dueDate ? String(item.dueDate).replace(/,/g, '') : '';
      const days = item.daysSinceIssue !== undefined ? item.daysSinceIssue : 0;
      
      csvContent += `${issueNum},${title},${name},${iDate},${dDate},${days}\n`;
    });
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', 'Active_Issues.csv');
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}

