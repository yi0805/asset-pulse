import { Component, ViewChild, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AssetUpsertRequest } from '../../models/api.models';
import { ApiError, mapApiError } from '../../shared/api-error';
import { AssetApiService } from '../asset-api.service';
import { AssetForm } from '../asset-form/asset-form';

@Component({
  selector: 'app-asset-create-page',
  imports: [RouterLink, AssetForm],
  templateUrl: './asset-create-page.html',
  styleUrl: './asset-create-page.css',
})
export class AssetCreatePage {
  private readonly api = inject(AssetApiService);
  private readonly router = inject(Router);
  @ViewChild(AssetForm) private form?: AssetForm;
  saving = false;
  error: ApiError | null = null;

  save(request: AssetUpsertRequest): void {
    if (this.saving) return;
    this.saving = true;
    this.error = null;
    this.api.createAsset(request).subscribe({
      next: (asset) => void this.router.navigate(['/assets', asset.id]),
      error: (response) => {
        this.error = mapApiError(response);
        this.form?.applyServerErrors(this.error.errors);
        this.saving = false;
      },
    });
  }
}
