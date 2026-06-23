import { Component } from '@angular/core';
import { ExecutionPlanService } from '../../core/services/execution-plan.service';
import { Store } from '@ngxs/store';
import { jwtDecode } from 'jwt-decode';
import { AuthState } from '../../store/auth.state';

@Component({
  selector: 'app-upload-plan',
  standalone:false,
  templateUrl: './upload-plan.html',
  styleUrls: ['./upload-plan.css']
})
export class UploadPlanComponent {
  selectedFile: File | null = null;
  isUploading = false;
  uploadMessage = '';

  constructor(private executionPlanService: ExecutionPlanService, private store: Store) {}

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  onUpload() {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadMessage = '';

    const token = this.store.selectSnapshot(AuthState.token);
    let userId = 0;
    if (token) {
        const decodedToken: any = jwtDecode(token);
        userId = decodedToken.userId || 0;
    }

    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('userId', userId.toString());

    this.executionPlanService.uploadPlan(formData).subscribe({
      next: (res: any) => {
        this.isUploading = false;
        this.uploadMessage = 'Plan uploaded successfully!';
        this.selectedFile = null;
      },
      error: (err) => {
        this.isUploading = false;
        this.uploadMessage = 'Failed to upload plan. Please try again.';
      }
    });
  }
}
