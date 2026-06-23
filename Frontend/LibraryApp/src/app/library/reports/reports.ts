import { Component } from '@angular/core';
import { ReportService } from '../../core/services/report.service';

@Component({
  selector: 'app-reports',
  standalone: false,
  templateUrl: './reports.html'
})
export class ReportsComponent {
  startDate: string = '';
  endDate: string = '';
  isDownloadingBooks = false;
  isDownloadingActiveIssues = false;
  isDownloadingHistory = false;

  constructor(private reportService: ReportService) {}

  downloadBooks() {
    this.isDownloadingBooks = true;
    this.reportService.downloadBooksReport().subscribe({
      next: (blob) => {
        this.triggerDownload(blob, `books_report_${this.getDateString()}.csv`);
        this.isDownloadingBooks = false;
      },
      error: () => this.isDownloadingBooks = false
    });
  }

  downloadActiveIssues() {
    this.isDownloadingActiveIssues = true;
    this.reportService.downloadActiveIssuesReport().subscribe({
      next: (blob) => {
        this.triggerDownload(blob, `active_issues_${this.getDateString()}.csv`);
        this.isDownloadingActiveIssues = false;
      },
      error: () => this.isDownloadingActiveIssues = false
    });
  }

  downloadHistory() {
    this.isDownloadingHistory = true;
    this.reportService.downloadIssueHistoryReport(this.startDate, this.endDate).subscribe({
      next: (blob) => {
        this.triggerDownload(blob, `issue_history_${this.getDateString()}.csv`);
        this.isDownloadingHistory = false;
      },
      error: () => this.isDownloadingHistory = false
    });
  }

  private triggerDownload(blob: Blob, filename: string) {
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }

  private getDateString() {
    const d = new Date();
    return `${d.getFullYear()}${(d.getMonth()+1).toString().padStart(2, '0')}${d.getDate().toString().padStart(2, '0')}`;
  }
}
