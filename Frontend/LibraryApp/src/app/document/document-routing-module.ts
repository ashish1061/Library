import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { UploadPlanComponent } from './upload-plan/upload-plan';

const routes: Routes = [
  { path: 'upload', component: UploadPlanComponent },
  { path: '', redirectTo: 'upload', pathMatch: 'full' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class DocumentRoutingModule {}
