import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MagazineService } from '../../core/services/magazine.service';
import { IssueService } from '../../core/services/issue.service';
import { ToastService } from '../../core/services/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-manage-magazines',
  templateUrl: './manage-magazines.html',
  styleUrls: ['./manage-magazines.css'],
  standalone: false
})
export class ManageMagazines implements OnInit {
  magazineForm!: FormGroup;
  coverImagePath: string | null = null;
  loadedmagazine: any = null;
  activeIssues: any[] = [];
  
  isSaving = false;
  isUploading = false;
  isChecking = false;
  lookupmagazineId: string | number = '';
  
  magazinesList: any[] = [];
  filteredMagazines: any[] = [];
  searchQuery: string = '';
  isLoadingList = false;
  showModal = false;
  isEditMode = false;
  showBulkModal = false;
  bulkUploadFile: File | null = null;

  constructor(
    private fb: FormBuilder, 
    private magazineService: MagazineService,
    private issueService: IssueService,
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadAllMagazines();
  }

  initForm() {
    this.magazineForm = this.fb.group({
      magazineId: ['', [Validators.required, Validators.min(1)]],
      title: ['', Validators.required],
      publisher: ['', Validators.required],
      issueDate: ['', Validators.required],
      rackLocation: [''],
      category: [''],
      totalCopies: [1, [Validators.required, Validators.min(1)]]
    });
  }

  loadAllMagazines() {
    this.isLoadingList = true;
    this.magazineService.getAllMagazines().subscribe({
      next: (res: any) => {
        this.magazinesList = Array.isArray(res) ? res : [];
        this.filteredMagazines = [...this.magazinesList];
        this.isLoadingList = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingList = false;
        this.toastService.error('Failed to load magazines list.');
        this.cdr.detectChanges();
      }
    });
  }

  onSearchChange() {
    if (!this.searchQuery) {
      this.filteredMagazines = [...this.magazinesList];
      return;
    }
    const q = this.searchQuery.toLowerCase();
    this.filteredMagazines = this.magazinesList.filter(m => 
      (m.title && m.title.toLowerCase().includes(q)) || 
      (m.publisher && m.publisher.toLowerCase().includes(q)) ||
      (m.magazineId && m.magazineId.toString().includes(q))
    );
  }

  openAddModal() {
    this.isEditMode = false;
    this.initForm();
    this.coverImagePath = null;
    this.loadedmagazine = null;
    this.showModal = true;
  }

  openEditModal(magazine: any) {
    this.isEditMode = true;
    this.loadedmagazine = magazine;
    this.coverImagePath = magazine.coverImagePath || magazine.CoverImagePath || null;
    this.magazineForm.patchValue({
      magazineId: magazine.magazineId || magazine.MagazineId,
      title: magazine.title || magazine.Title,
      publisher: magazine.publisher || magazine.Publisher,
      issueDate: magazine.issueDate || magazine.IssueDate,
      rackLocation: magazine.rackLocation || magazine.RackLocation,
      category: magazine.category || magazine.Category,
      totalCopies: magazine.totalCopies || magazine.TotalCopies || 1
    });
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.isUploading = true;
      this.magazineService.uploadCover(file).subscribe({
        next: (res: any) => {
          this.coverImagePath = res.imagePath;
          this.toastService.success('Cover image uploaded successfully.');
          this.isUploading = false;
          this.cdr.markForCheck();
        },
        error: (err) => {
          console.error(err);
          this.toastService.error('Failed to upload cover image.');
          this.isUploading = false;
          this.cdr.markForCheck();
        }
      });
    }
  }

  onSubmit(): void {
    if (this.magazineForm.invalid) {
      this.toastService.error('Please fill all required fields correctly.');
      return;
    }

    this.isSaving = true;
    const formVal = this.magazineForm.value;
    
    const payload = {
      MagazineId: parseInt(formVal.magazineId, 10),
      Title: formVal.title,
      Publisher: formVal.publisher,
      IssueDate: formVal.issueDate,
      RackLocation: formVal.rackLocation,
      Category: formVal.category,
      TotalCopies: parseInt(formVal.totalCopies, 10),
      AvailableCopies: parseInt(formVal.totalCopies, 10), // simplified
      CoverImagePath: this.coverImagePath || ''
    };

    if (this.isEditMode) {
      this.magazineService.updateMagazine(payload.MagazineId, payload).subscribe({
        next: () => {
          this.toastService.success('Magazine updated successfully!');
          this.isSaving = false;
          this.closeModal();
          this.loadAllMagazines();
        },
        error: (err) => {
          console.error(err);
          this.toastService.error('Failed to update magazine.');
          this.isSaving = false;
        }
      });
    } else {
      this.magazineService.addMagazine(payload).subscribe({
        next: () => {
          this.toastService.success('Magazine added successfully!');
          this.isSaving = false;
          this.closeModal();
          this.loadAllMagazines();
        },
        error: (err) => {
          console.error(err);
          this.toastService.error('Failed to add magazine.');
          this.isSaving = false;
        }
      });
    }
  }

  deleteMagazine(id: number) {
    if (!confirm('Are you sure you want to delete this magazine?')) return;
    
    this.magazineService.deleteMagazine(id).subscribe({
      next: () => {
        this.toastService.success('Magazine deleted successfully.');
        this.loadAllMagazines();
      },
      error: () => {
        this.toastService.error('Failed to delete magazine. It might be currently issued.');
      }
    });
  }

  getCoverUrl(path: string | null): string {
    if (!path) return 'assets/images/placeholder_book.png';
    return `${environment.documentBaseUrl}${path}`;
  }

  openBulkModal() {
    this.showBulkModal = true;
    this.bulkUploadFile = null;
  }
  
  closeBulkModal() {
    this.showBulkModal = false;
  }

  onBulkFileSelected(event: any) {
    if (event.target.files.length > 0) {
      this.bulkUploadFile = event.target.files[0];
    }
  }

  uploadBulk() {
    if (!this.bulkUploadFile) {
      this.toastService.error('Please select a CSV file first.');
      return;
    }
    
    this.isUploading = true;
    this.magazineService.bulkUpload(this.bulkUploadFile).subscribe({
      next: () => {
        this.toastService.success('Bulk upload processed successfully.');
        this.isUploading = false;
        this.closeBulkModal();
        this.loadAllMagazines();
      },
      error: (err) => {
        this.toastService.error(err.error?.Message || 'Bulk upload failed.');
        this.isUploading = false;
      }
    });
  }
}
