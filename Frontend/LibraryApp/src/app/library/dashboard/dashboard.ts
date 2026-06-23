import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ChartConfiguration, ChartOptions, Chart, registerables } from 'chart.js';
import { IssueService } from '../../core/services/issue.service';
import { AnalyticsService, DashboardSummary, CategoryDistribution, IssueTrend } from '../../core/services/analytics.service';



@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  activeIssues: any[] = [];
  summary: DashboardSummary | null = null;
  
  // Pagination & Search
  searchTerm: string = '';
  currentPage: number = 1;
  itemsPerPage: number = 20;

  get filteredActiveIssues() {
    if (!this.searchTerm.trim()) {
      return this.activeIssues;
    }
    const term = this.searchTerm.toLowerCase().trim();
    return this.activeIssues.filter(item => 
      (item.bookName && item.bookName.toLowerCase().includes(term)) ||
      (item.empName && item.empName.toLowerCase().includes(term)) ||
      (item.issueNumber && item.issueNumber.toString().includes(term))
    );
  }

  get pagedActiveIssues() {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    return this.filteredActiveIssues.slice(startIndex, startIndex + this.itemsPerPage);
  }

  get totalPages() {
    return Math.max(1, Math.ceil(this.filteredActiveIssues.length / this.itemsPerPage));
  }

  onSearchChange() {
    this.currentPage = 1; // Reset to page 1 on search
  }

  nextPage() {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }

  prevPage() {
    if (this.currentPage > 1) this.currentPage--;
  }
  
  // Doughnut Chart (Category Distribution)
  public categoryChartData: ChartConfiguration<'doughnut'>['data'] = {
    labels: [],
    datasets: [ { data: [] } ]
  };
  public categoryChartOptions: ChartOptions<'doughnut'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: 'right' }
    }
  };

  // Line Chart (Issue Trends)
  public trendChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Books Issued',
        fill: true,
        tension: 0.4,
        borderColor: '#4f46e5',
        backgroundColor: 'rgba(79, 70, 229, 0.2)'
      }
    ]
  };
  public trendChartOptions: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    }
  };

  constructor(
    private issueService: IssueService,
    private analyticsService: AnalyticsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // 1. Fetch Summary KPIs
    this.analyticsService.getSummary().subscribe({
      next: (res: any) => {
        this.summary = {
          totalBooks: res.totalBooks ?? res.TotalBooks ?? 0,
          activeIssues: res.activeIssues ?? res.ActiveIssues ?? 0,
          registeredMembers: res.registeredMembers ?? res.RegisteredMembers ?? 0,
          totalIssues: res.totalIssues ?? res.TotalIssues ?? 0
        };
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching dashboard summary:', err)
    });

    // 2. Fetch Category Distribution for Doughnut chart
    this.analyticsService.getBooksByCategory().subscribe({
      next: (res: any[]) => {
        // Top 5 categories + "Other"
        let top = res.slice(0, 5);
        let otherCount = res.slice(5).reduce((acc, curr) => acc + (curr.count ?? curr.Count ?? 0), 0);
        
        const labels = top.map(c => c.category ?? c.Category ?? 'Unknown');
        const data = top.map(c => c.count ?? c.Count ?? 0);
        
        if (otherCount > 0) {
          labels.push('Other');
          data.push(otherCount);
        }

        this.categoryChartData = {
          labels,
          datasets: [{
            data,
            backgroundColor: ['#4f46e5', '#ec4899', '#8b5cf6', '#10b981', '#f59e0b', '#6b7280']
          }]
        };
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching books by category:', err)
    });

    // 3. Fetch Issue Trends for Line chart
    this.analyticsService.getIssueTrends().subscribe({
      next: (res: any[]) => {
        this.trendChartData = {
          labels: res.map(t => t.month ?? t.Month ?? ''),
          datasets: [{
            ...this.trendChartData.datasets[0],
            data: res.map(t => t.issueCount ?? t.IssueCount ?? 0)
          }]
        };
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching issue trends:', err)
    });

    // 4. Fetch Active Issues table
    this.issueService.getActiveIssues().subscribe({
      next: (res: any) => {
        const issuesList = Array.isArray(res) ? res : [];
        this.activeIssues = issuesList.map(item => ({
          issueNumber: item.issueNumber ?? item.IssueNumber,
          bookName: item.bookName ?? item.BookName,
          empName: item.empName ?? item.EmpName,
          issueDate: item.issueDate ?? item.IssueDate
        }));
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error fetching active issues:', err)
    });
  }
}
