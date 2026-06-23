import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { DocumentRoutingModule } from './document-routing-module';
import { UploadPlanComponent } from './upload-plan/upload-plan';

@NgModule({
  declarations: [
    UploadPlanComponent
  ],
  imports: [CommonModule, DocumentRoutingModule],
})
export class DocumentModule {}
