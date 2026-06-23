import { Component, OnInit } from '@angular/core';
import { EmployeeService } from '../../core/services/employee.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-manage-employees',
  standalone: false,
  templateUrl: './manage-employees.html',
  styleUrl: './manage-employees.css',
})
export class ManageEmployees implements OnInit {
  employees: any[] = [];
  isLoading = false;
  isSyncing = false;

  // Bulk Upload Modal
  showBulkModal = false;
  bulkUploadFile: File | null = null;
  isUploading = false;

  // Search and Pagination
  searchQuery = '';
  currentPage = 1;
  pageSize = 20;

  get filteredAndPagedEmployees() {
    let result = this.employees;
    
    // 1. Filter
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(emp => 
        (emp.empName && emp.empName.toLowerCase().includes(q)) ||
        (emp.empID && emp.empID.toLowerCase().includes(q)) ||
        (emp.emailid && emp.emailid.toLowerCase().includes(q)) ||
        (emp.department && emp.department.toLowerCase().includes(q)) ||
        (emp.designation && emp.designation.toLowerCase().includes(q))
      );
    }
    
    // 2. Pagination
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return result.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages() {
    let result = this.employees;
    if (this.searchQuery) {
      const q = this.searchQuery.toLowerCase();
      result = result.filter(emp => 
        (emp.empName && emp.empName.toLowerCase().includes(q)) ||
        (emp.empID && emp.empID.toLowerCase().includes(q)) ||
        (emp.emailid && emp.emailid.toLowerCase().includes(q))
      );
    }
    return Math.ceil(result.length / this.pageSize);
  }

  // Drawer / Detail Section State
  selectedEmployee: any = null;
  isDrawerOpen = false;

  // Add Employee Modal State
  isAddEmployeeModalOpen = false;
  isAdding = false;
  newEmployee: any = { empID: '', empName: '', emailid: '', mobile: '', department: '', designation: '' };

  constructor(
    private employeeService: EmployeeService,
    private authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    const cached = localStorage.getItem('cached_employees_raw');
    if (cached) {
      try {
        const rawList = JSON.parse(cached);
        if (Array.isArray(rawList)) {
          this.employees = this.mapEmployees(rawList);
        }
      } catch (e) {
        console.error('Failed to parse cached employees', e);
      }
    }
    this.loadEmployees();
  }

  mapEmployees(rawList: any[]): any[] {
    return rawList.map((emp: any) => {
      let path = emp.imagePath || emp.ImagePath || null;
      if (path && !path.startsWith('http') && !path.startsWith('data:')) {
        path = `https://localhost:7004${path}`;
      }
      
      const isAdmin = emp.isAdmin === true || emp.IsAdmin === true;
      return {
        ...emp,
        imagePath: path,
        department: emp.department || emp.Department || '',
        designation: emp.designation || emp.Designation || '',
        isAdmin: isAdmin,
        IsAdmin: isAdmin
      };
    });
  }

  loadEmployees() {
    this.isLoading = this.employees.length === 0;
    this.employeeService.getAllEmployees().subscribe({
      next: (res: any) => {
        this.employees = this.mapEmployees(res);
        localStorage.setItem('cached_employees_raw', JSON.stringify(res));
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  syncDarwinbox() {
    this.isSyncing = true;
    
    this.employeeService.syncDarwinbox().subscribe({
      next: (res: any) => {
        this.toastService.success(res.message || 'Sync successful');
        this.isSyncing = false;
        this.loadEmployees(); // Reload after sync
      },
      error: (err) => {
        console.error(err);
        this.isSyncing = false;
      }
    });
  }

  saveAll() {
    this.isLoading = true;
    
    // Ensure both formats (casing) are matched when sending to backend
    const payload = this.employees.map(emp => {
      let path = emp.imagePath;
      // Strip off the documentBaseUrl before saving to the DB so it stores as a relative path (/images/...)
      if (path && path.startsWith('https://localhost:7004')) {
        path = path.substring('https://localhost:7004'.length);
      }
      const isAdmin = emp.isAdmin === true || emp.IsAdmin === true;
      return {
        ...emp,
        ImagePath: path,
        Department: emp.department || emp.Department,
        Designation: emp.designation || emp.Designation,
        IsAdmin: isAdmin,
        isAdmin: isAdmin
      };
    });

    this.employeeService.bulkUpdateEmployees(payload).subscribe({
      next: (res: any) => {
        this.toastService.success(res.message || 'Changes saved successfully');
        this.isLoading = false;
        this.loadEmployees(); // Reload to refresh visual references
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  // Drawer Operations
  openEmployeeDetails(emp: any) {
    this.selectedEmployee = emp;
    this.isDrawerOpen = true;
  }

  closeDrawer() {
    this.isDrawerOpen = false;
    this.selectedEmployee = null;
  }

  // --- Add Employee Logic ---
  openAddEmployeeModal() {
    this.newEmployee = { empID: '', empName: '', emailid: '', mobile: '', department: '', designation: '' };
    this.isAddEmployeeModalOpen = true;
  }

  closeAddEmployeeModal() {
    this.isAddEmployeeModalOpen = false;
  }

  saveNewEmployee() {
    if (!this.newEmployee.empID || !this.newEmployee.empName || !this.newEmployee.emailid) {
      this.toastService.error("Employee ID, Name, and Email are required.");
      return;
    }

    this.isAdding = true;
    this.employeeService.addEmployee(this.newEmployee).subscribe({
      next: () => {
        this.toastService.success("Employee added successfully!");
        this.closeAddEmployeeModal();
        this.loadEmployees(); // Refresh list
        this.isAdding = false;
      },
      error: (err: any) => {
        const errorMsg = err.error?.message || err.error?.Message || "Failed to add employee.";
        this.toastService.error(errorMsg);
        this.isAdding = false;
      }
    });
  }

  getInitials(name: string): string {
    if (!name) return 'EE';
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return parts[0].slice(0, 2).toUpperCase();
  }

  fetchFromDarwinbox() {
    if (!this.selectedEmployee) return;
    this.selectedEmployee.isFetchingDarwinbox = true;
    
    this.employeeService.getProfilePicFromDarwinbox(this.selectedEmployee.empID).subscribe({
      next: (res) => {
        let imageData = res.data;
        // Check if data is not empty, if so parse it
        if (imageData) {
          try {
            const parsed = JSON.parse(imageData);
            if (parsed.data) imageData = parsed.data; // Sometimes they wrap base64 in { data: "..." }
          } catch (e) {
            // Probably not JSON, it's just raw base64 or something
          }
          
          if (!imageData.startsWith('data:image')) {
             imageData = 'data:image/jpeg;base64,' + imageData;
          }
          this.selectedEmployee.imagePath = imageData;
          this.toastService.success(`Fetched official image for ${this.selectedEmployee.empName} from Darwinbox!`);
        } else {
          this.toastService.error(`No image found in Darwinbox for ${this.selectedEmployee.empName}.`);
        }
        this.selectedEmployee.isFetchingDarwinbox = false;
      },
      error: (err) => {
        console.error(err);
        this.toastService.error(`Failed to fetch image from Darwinbox.`);
        this.selectedEmployee.isFetchingDarwinbox = false;
      }
    });
  }

  onImageSelected(event: any) {
    const file = event.target.files[0];
    if (!file || !this.selectedEmployee) return;

    this.employeeService.uploadAvatar(file).subscribe({
      next: (res: any) => {
        // Assume API returns { imagePath: "/images/guid_filename.jpg" }
        const path = res?.imagePath || res?.ImagePath || '';
        this.selectedEmployee.imagePath = `https://localhost:7004${path}`;
        this.toastService.success(`Uploaded new image for ${this.selectedEmployee.empName} successfully!`);
      },
      error: () => {
        this.toastService.error(`Failed to upload image for ${this.selectedEmployee.empName}.`);
      }
    });
  }

  resetPassword() {
    if (!this.selectedEmployee) return;

    const newPassword = prompt(`Enter a new password for ${this.selectedEmployee.empName} (min 6 characters):`);
    if (!newPassword) return;

    if (newPassword.length < 6) {
      this.toastService.error('Password must be at least 6 characters long.');
      return;
    }

    if (confirm(`Are you sure you want to reset the password for ${this.selectedEmployee.empName}? They will receive an email with their new password.`)) {
      this.authService.adminChangePassword({
        empId: this.selectedEmployee.empID,
        newPassword: newPassword
      }).subscribe({
        next: (res: any) => {
          this.toastService.success(res.message || 'Password reset successfully');
        },
        error: (err: any) => {
          this.toastService.error(err.error?.message || 'Failed to reset password');
        }
      });
    }
  }

  openBulkModal() {
    this.showBulkModal = true;
    this.bulkUploadFile = null;
  }

  closeBulkModal() {
    this.showBulkModal = false;
    this.bulkUploadFile = null;
  }

  onBulkFileSelected(event: any) {
    this.bulkUploadFile = event.target.files?.[0] || null;
  }

  onBulkUpload(event: any) {
    const file: File = event.target.files?.[0];
    if (!file) return;
    this.bulkUploadFile = file;
    this.uploadBulk();
  }

  uploadBulk() {
    if (!this.bulkUploadFile) return;
    this.isUploading = true;

    this.employeeService.bulkUpload(this.bulkUploadFile).subscribe({
      next: (res: any) => {
        this.isUploading = false;
        this.toastService.success(res.message || 'Employees uploaded successfully');
        this.loadEmployees();
        this.closeBulkModal();
      },
      error: (err: any) => {
        this.isUploading = false;
        this.toastService.error(err.error?.message || 'Failed to upload employees');
      }
    });
  }

  toggleAdmin(employee: any, event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    
    // Optimistic UI update
    const previousState = employee.isAdmin;
    employee.isAdmin = isChecked;
    employee.IsAdmin = isChecked;

    this.employeeService.toggleAdmin(employee.empID, isChecked).subscribe({
      next: () => {
        this.toastService.success(`Admin access ${isChecked ? 'granted' : 'revoked'} for ${employee.empName}`);
      },
      error: () => {
        // Revert on failure
        employee.isAdmin = previousState;
        employee.IsAdmin = previousState;
        (event.target as HTMLInputElement).checked = previousState;
        this.toastService.error('Failed to update admin role. Please try again.');
      }
    });
  }

  downloadCompleteCsv() {
    if (!this.employees || this.employees.length === 0) {
      this.toastService.warning('No employees available to download.');
      return;
    }

    const headers = ['EmpID', 'EmpName', 'password', 'emailid', 'mobile', 'Department', 'Designation'];
    
    const rows = this.employees.map(emp => {
      return [
        emp.empID || emp.EmpID || '',
        emp.empName || emp.EmpName || '',
        'Welcome@123', // Dummy password for CSV export since actual is hashed
        emp.emailid || emp.email || emp.Email || '',
        emp.mobile || emp.Mobile || '',
        emp.department || emp.Department || '',
        emp.designation || emp.Designation || ''
      ].map(val => `"${val}"`).join(',');
    });

    const csvContent = [headers.join(','), ...rows].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'complete-employees.csv';
    link.click();
  }
}

