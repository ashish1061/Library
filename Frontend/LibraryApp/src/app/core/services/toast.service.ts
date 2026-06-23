import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
}

export interface ConfirmRequest {
  message: string;
  resolve: (value: boolean) => void;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  toasts$: Observable<Toast[]> = this.toastsSubject.asObservable();

  private confirmSubject = new BehaviorSubject<ConfirmRequest | null>(null);
  confirm$: Observable<ConfirmRequest | null> = this.confirmSubject.asObservable();

  private nextId = 1;

  constructor(private ngZone: NgZone) {}

  success(message: string) {
    this.show(message, 'success');
  }

  error(message: string) {
    this.show(message, 'error');
  }

  info(message: string) {
    this.show(message, 'info');
  }

  warning(message: string) {
    this.show(message, 'warning');
  }

  confirm(message: string): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      // Broadcast confirm request to the global ToastComponent
      this.confirmSubject.next({
        message,
        resolve: (result: boolean) => {
          // Force execution inside Angular Zone so async/await calling contexts resume inside the zone
          this.ngZone.run(() => {
            this.confirmSubject.next(null); // Clear active confirm dialog request
            resolve(result);
          });
        }
      });
    });
  }

  private show(message: string, type: Toast['type']) {
    this.ngZone.run(() => {
      const id = this.nextId++;
      const currentToasts = this.toastsSubject.value;
      
      // Add new toast to stack
      this.toastsSubject.next([...currentToasts, { id, message, type }]);

      // Auto-remove after 4 seconds
      setTimeout(() => {
        this.remove(id);
      }, 4000);
    });
  }

  remove(id: number) {
    this.ngZone.run(() => {
      const filtered = this.toastsSubject.value.filter(t => t.id !== id);
      this.toastsSubject.next(filtered);
    });
  }
}
