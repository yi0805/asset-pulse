import { Routes } from '@angular/router';
import { NotFoundPage } from './shared/not-found-page/not-found-page';
import { PlaceholderPage } from './shared/placeholder-page/placeholder-page';
import { AssetsPage } from './assets/assets-page/assets-page';
import { AssetDetailPage } from './assets/asset-detail-page/asset-detail-page';
import { AssetCreatePage } from './assets/asset-create-page/asset-create-page';
import { AssetEditPage } from './assets/asset-edit-page/asset-edit-page';

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
    component: AssetsPage,
  },
  {
    path: 'assets/new',
    component: AssetCreatePage,
  },
  {
    path: 'assets/:id/edit',
    component: AssetEditPage,
  },
  {
    path: 'assets/:id',
    component: AssetDetailPage,
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
