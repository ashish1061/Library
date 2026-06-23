import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RoleGuard } from './core/guards/role.guard';

const routes: Routes = [
  { path: 'auth', loadChildren: () => import('./auth/auth-module').then(m => m.AuthModule) },
  { path: 'admin', loadChildren: () => import('./admin/admin-module').then(m => m.AdminModule), canActivate: [RoleGuard], data: { expectedRole: 'Admin' } },
  { path: 'library', loadChildren: () => import('./library/library-module').then(m => m.LibraryModule), canActivate: [RoleGuard] },
  { path: 'document', loadChildren: () => import('./document/document-module').then(m => m.DocumentModule), canActivate: [RoleGuard] },
  { path: '', redirectTo: '/auth/login', pathMatch: 'full' },
  { path: '**', redirectTo: 'auth/login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
