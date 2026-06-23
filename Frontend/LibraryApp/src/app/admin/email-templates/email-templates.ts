import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { EmailTemplateService, EmailTemplate } from '../../core/services/email-template.service';

@Component({
  selector: 'app-email-templates',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './email-templates.html',
  styleUrls: ['./email-templates.css']
})
export class EmailTemplatesComponent implements OnInit {
  templates: EmailTemplate[] = [];
  loading = false;
  error = '';
  
  // Modal state
  showModal = false;
  isEditing = false;
  currentTemplateId: number | null = null;
  templateForm: FormGroup;
  saving = false;

  constructor(
    private templateService: EmailTemplateService,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder
  ) {
    this.templateForm = this.fb.group({
      purpose: ['', Validators.required],
      subject: ['', Validators.required],
      body: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  loadTemplates() {
    this.loading = true;
    this.templateService.getAllTemplates().subscribe({
      next: (data) => {
        console.log('Templates received from API:', data);
        this.templates = data || [];
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error fetching templates:', err);
        this.error = 'Failed to load email templates.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openAddModal() {
    this.isEditing = false;
    this.currentTemplateId = null;
    this.templateForm.reset();
    this.showModal = true;
  }

  openEditModal(template: EmailTemplate) {
    this.isEditing = true;
    this.currentTemplateId = template.templateId;
    this.templateForm.patchValue({
      purpose: template.purpose,
      subject: template.subject,
      body: template.body
    });
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.templateForm.reset();
    this.error = '';
  }

  saveTemplate() {
    if (this.templateForm.invalid) {
      this.templateForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    const templateData: EmailTemplate = {
      templateId: this.currentTemplateId || 0,
      ...this.templateForm.value
    };

    if (this.isEditing && this.currentTemplateId) {
      this.templateService.updateTemplate(this.currentTemplateId, templateData).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadTemplates();
        },
        error: (err) => {
          console.error(err);
          this.error = 'Failed to update template.';
          this.saving = false;
          this.cdr.detectChanges();
        }
      });
    } else {
      this.templateService.addTemplate(templateData).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.loadTemplates();
        },
        error: (err) => {
          console.error(err);
          this.error = 'Failed to create template.';
          this.saving = false;
          this.cdr.detectChanges();
        }
      });
    }
  }
}
