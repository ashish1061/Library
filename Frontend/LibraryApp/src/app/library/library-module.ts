import { NgModule } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { Dashboard } from './dashboard/dashboard';
import { BookSearch } from './book-search/book-search';
import { IssueBook } from './issue-book/issue-book';
import { ReturnBook } from './return-book/return-book';
import { IssueHistory } from './issue-history/issue-history';
import { ReportsComponent } from './reports/reports';
import { RequestIssue } from './request-issue/request-issue';
import { MyHistory } from './my-history/my-history';

const routes: Routes = [
  { path: 'dashboard', component: Dashboard },
  { path: 'book-search', component: BookSearch },
  { path: 'issue-book', component: IssueBook },
  { path: 'return-book', component: ReturnBook },
  { path: 'issue-history', component: IssueHistory },
  { path: 'reports', component: ReportsComponent },
  { path: 'request-issue', component: RequestIssue },
  { path: 'my-history', component: MyHistory },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];

@NgModule({
  declarations: [
    Dashboard,
    BookSearch,
    IssueBook,
    ReturnBook,
    IssueHistory,
    ReportsComponent,
    RequestIssue,
    MyHistory,
  ],
  imports: [CommonModule, FormsModule, RouterModule.forChild(routes), BaseChartDirective],
})
export class LibraryModule {}
