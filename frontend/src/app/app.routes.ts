import { Routes } from '@angular/router';
import { NotFoundPage } from './shared/not-found-page/not-found-page';
import { PlaceholderPage } from './shared/placeholder-page/placeholder-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dashboard',
    component: PlaceholderPage,
    data: {
      title: 'Dashboard',
      description:
        'The dashboard is not complete yet. Monitoring summaries will be added in Task 007.',
    },
  },
  {
    path: 'assets',
    component: PlaceholderPage,
    data: {
      title: 'Assets',
      description: 'Asset management is not complete yet. It will be added in Task 005.',
    },
  },
  {
    path: 'assets/new',
    component: PlaceholderPage,
    data: {
      title: 'Create asset',
      description: 'Asset creation is not complete yet. It will be added in Task 005.',
    },
  },
  {
    path: 'assets/:id/edit',
    component: PlaceholderPage,
    data: {
      title: 'Edit asset',
      description: 'Asset editing is not complete yet. It will be added in Task 005.',
    },
  },
  {
    path: 'assets/:id',
    component: PlaceholderPage,
    data: {
      title: 'Asset detail',
      description: 'Asset details are not complete yet. They will be added in Task 005.',
    },
  },
  {
    path: 'alarms',
    component: PlaceholderPage,
    data: {
      title: 'Alarms',
      description: 'Alarm management is not complete yet. It will be added in Task 006.',
    },
  },
  { path: '**', component: NotFoundPage },
];
