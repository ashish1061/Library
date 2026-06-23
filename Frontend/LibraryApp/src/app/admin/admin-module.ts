import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ManageBooks } from './manage-books/manage-books';
import { ManageMagazines } from './manage-magazines/manage-magazines';
import { ManageEmployees } from './manage-employees/manage-employees';
import { ApproveRequests } from './approve-requests/approve-requests';
import { EmailTemplatesComponent } from './email-templates/email-templates';
import { ManageMfa } from './manage-mfa/manage-mfa';

const routes: Routes = [
  { path: 'manage-books', component: ManageBooks },
  { path: 'manage-magazines', component: ManageMagazines },
  { path: 'manage-employees', component: ManageEmployees },
  { path: 'manage-mfa', component: ManageMfa },
  { path: 'approve-requests', component: ApproveRequests },
  { path: 'email-templates', component: EmailTemplatesComponent },
  { path: '', redirectTo: 'manage-books', pathMatch: 'full' },
];

@NgModule({
  declarations: [ManageBooks, ManageMagazines, ManageEmployees, ApproveRequests, ManageMfa],
  imports: [CommonModule, RouterModule.forChild(routes), ReactiveFormsModule, FormsModule, EmailTemplatesComponent],
})
export class AdminModule {}
