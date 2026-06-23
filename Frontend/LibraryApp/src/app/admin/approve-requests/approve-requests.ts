import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-approve-requests',
  standalone: false,
  templateUrl: './approve-requests.html',
  styleUrl: './approve-requests.css',
})
export class ApproveRequests implements OnInit {
  documentBaseUrl = environment.documentBaseUrl;
  requests: any[] = [];
  filteredRequests: any[] = [];
  isLoading = false;
  isProcessing = false;

  keyword: string = '';
  
  // Pagination
  currentPage: number = 1;
  pageSize: number = 10;

  get pagedRequests() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredRequests.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    return Math.ceil(this.filteredRequests.length / this.pageSize) || 1;
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
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadPendingRequests();
  }

  loadPendingRequests() {
    this.isLoading = true;
    this.issueService.getPendingRequests().subscribe({
      next: (res) => {
        this.requests = res.map(r => ({ ...r, selected: false }));
        this.applyFilters();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to load pending requests.');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  applyFilters() {
    let temp = this.requests;
    if (this.keyword.trim() !== '') {
      const k = this.keyword.toLowerCase();
      temp = temp.filter(r => 
        (r.empID && r.empID.toLowerCase().includes(k)) ||
        (r.empName && r.empName.toLowerCase().includes(k)) ||
        (r.itemName && r.itemName.toLowerCase().includes(k))
      );
    }
    this.filteredRequests = temp;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  onSearchChange() {
    this.applyFilters();
  }

  get hasSelected() {
    return this.filteredRequests.some(r => r.selected);
  }

  get allSelected() {
    return this.filteredRequests.length > 0 && this.filteredRequests.every(r => r.selected);
  }

  toggleAll(event: any) {
    const checked = event.target.checked;
    this.filteredRequests.forEach(r => r.selected = checked);
  }

  approveSelected() {
    const selectedIds = this.filteredRequests.filter(r => r.selected).map(r => r.requestID || r.requestId);
    if (selectedIds.length === 0) return;

    this.isProcessing = true;
    this.issueService.approveRequests(selectedIds).subscribe({
      next: () => {
        this.toastService.success(`${selectedIds.length} request(s) approved and issued successfully.`);
        this.loadPendingRequests();
        this.isProcessing = false;
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to approve requests.');
        this.isProcessing = false;
      }
    });
  }

  rejectSelected() {
    const selectedIds = this.filteredRequests.filter(r => r.selected).map(r => r.requestID || r.requestId);
    if (selectedIds.length === 0) return;

    this.isProcessing = true;
    this.issueService.rejectRequests(selectedIds).subscribe({
      next: () => {
        this.toastService.success(`${selectedIds.length} request(s) rejected successfully.`);
        this.loadPendingRequests();
        this.isProcessing = false;
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Failed to reject requests.');
        this.isProcessing = false;
      }
    });
  }
}
