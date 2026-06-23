import { Component, OnInit } from '@angular/core';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

@Component({
  selector: 'app-issue-history',
  standalone: false,
  templateUrl: './issue-history.html',
  styleUrl: './issue-history.css',
})
export class IssueHistory implements OnInit {
  catalogBaseUrl = environment.catalogBaseUrl;
  history: any[] = [];
  filteredHistory: any[] = [];
  startDate: string = '';
  endDate: string = '';
  isLoading = false;

  // Search and Pagination
  searchText = '';
  currentPage = 1;
  pageSize = 20;

  constructor(private issueService: IssueService) {}

  ngOnInit() {
    this.loadHistory();
  }

  loadHistory() {
    this.issueService.getIssueHistory().subscribe({
      next: (res: any) => {
        const mappedRes = res.map((item: any) => ({
          ...item,
          empName: item.empName || item.EmpName,
          bookName: item.bookName || item.BookName,
          issueDate: item.issueDate || item.IssueDate,
          dueDate: item.dueDate || item.DueDate,
          returnDate: item.returnDate || item.ReturnDate,
          coverImagePath: item.coverImagePath || item.CoverImagePath,
          employeeImagePath: item.employeeImagePath || item.EmployeeImagePath,
          itemType: item.itemType || item.ItemType || 'Book'
        }));
        this.history = mappedRes;
        this.filteredHistory = mappedRes;
      },
      error: (err) => console.error(err)
    });
  }

  search() {
    this.currentPage = 1;
    if (!this.startDate || !this.endDate) {
      this.filteredHistory = this.history;
      return;
    }
    
    const start = new Date(this.startDate);
    const end = new Date(this.endDate);
    // Ensure end date includes the full day
    end.setHours(23, 59, 59, 999);

    this.filteredHistory = this.history.filter(item => {
      if (!item.issueDate) return false;
      const issueDate = new Date(item.issueDate);
      return issueDate >= start && issueDate <= end;
    });
  }

  get paginatedHistory() {
    let result = this.filteredHistory;

    if (this.searchText.trim()) {
      const q = this.searchText.toLowerCase();
      result = result.filter(item => 
        (item.issueNumber && item.issueNumber.toString().toLowerCase().includes(q)) ||
        (item.bookName && item.bookName.toLowerCase().includes(q)) ||
        (item.empName && item.empName.toLowerCase().includes(q))
      );
    }

    const startIndex = (this.currentPage - 1) * this.pageSize;
    return result.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    let result = this.filteredHistory;
    if (this.searchText.trim()) {
      const q = this.searchText.toLowerCase();
      result = result.filter(item => 
        (item.issueNumber && item.issueNumber.toString().toLowerCase().includes(q)) ||
        (item.bookName && item.bookName.toLowerCase().includes(q)) ||
        (item.empName && item.empName.toLowerCase().includes(q))
      );
    }
    return Math.ceil(result.length / this.pageSize) || 1;
  }

  get totalFilteredLength() {
    let result = this.filteredHistory;
    if (this.searchText.trim()) {
      const q = this.searchText.toLowerCase();
      result = result.filter(item => 
        (item.issueNumber && item.issueNumber.toString().toLowerCase().includes(q)) ||
        (item.bookName && item.bookName.toLowerCase().includes(q)) ||
        (item.empName && item.empName.toLowerCase().includes(q))
      );
    }
    return result.length;
  }

  onSearchTextChange() {
    this.currentPage = 1;
  }

  onPageSizeChange() {
    this.currentPage = 1;
  }

  prevPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  nextPage() {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
    }
  }

  exportExcel() {
    this.isLoading = true;
    
    this.issueService.exportIssues(this.startDate, this.endDate).subscribe({
      next: (res: Blob) => {
        const a = document.createElement('a');
        const objectUrl = URL.createObjectURL(res);
        a.href = objectUrl;
        a.download = 'IssueHistory.xlsx';
        a.click();
        URL.revokeObjectURL(objectUrl);
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  exportPdf() {
    const doc = new jsPDF();
    
    doc.setFontSize(18);
    doc.text('Issue History Report', 14, 22);
    doc.setFontSize(11);
    doc.setTextColor(100);
    doc.text(`Generated on: ${new Date().toLocaleDateString()}`, 14, 30);
    
    if (this.startDate && this.endDate) {
      doc.text(`Period: ${this.startDate} to ${this.endDate}`, 14, 36);
    }
    
    const tableData = this.filteredHistory.map(item => [
      item.issueNumber,
      item.bookName,
      item.empName,
      item.issueDate,
      item.returnDate || 'Not Returned'
    ]);
    
    autoTable(doc, {
      head: [['Issue #', 'Book Name', 'Issued To', 'Issue Date', 'Return Date']],
      body: tableData,
      startY: 40,
      theme: 'grid',
      styles: { fontSize: 9 },
      headStyles: { fillColor: [79, 70, 229] }
    });
    
    doc.save('IssueHistory_Report.pdf');
  }

}
