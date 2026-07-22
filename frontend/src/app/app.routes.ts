import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'management', pathMatch: 'full' },
  {
    path: 'management',
    loadComponent: () => import('./features/management/management').then(m => m.Management)
  }
];
