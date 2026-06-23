import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { ToastService, Toast, ConfirmRequest } from '../../services/toast.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-toast',
  templateUrl: './toast.html',
  styleUrls: ['./toast.css'],
  standalone: false
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  activeConfirm: ConfirmRequest | null = null;
  private subs = new Subscription();

  constructor(
    private toastService: ToastService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // Subscribe to stackable notifications stream
    this.subs.add(
      this.toastService.toasts$.subscribe(toasts => {
        this.toasts = toasts;
        this.cdr.markForCheck();
      })
    );

    // Subscribe to confirmation requests stream
    this.subs.add(
      this.toastService.confirm$.subscribe(confirmRequest => {
        this.activeConfirm = confirmRequest;
        this.cdr.markForCheck();
      })
    );
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  removeToast(id: number) {
    this.toastService.remove(id);
  }

  resolveConfirm(result: boolean) {
    if (this.activeConfirm) {
      this.activeConfirm.resolve(result);
    }
  }
}
