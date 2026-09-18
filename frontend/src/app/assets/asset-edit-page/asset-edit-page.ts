import { Component, ViewChild, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Asset, AssetUpsertRequest } from '../../models/api.models';
import { ApiError, mapApiError } from '../../shared/api-error';
import { ErrorState } from '../../shared/states/error-state';
import { LoadingState } from '../../shared/states/loading-state';
import { AssetApiService } from '../asset-api.service';
import { AssetForm } from '../asset-form/asset-form';

@Component({
  selector: 'app-asset-edit-page',
  imports: [AssetForm, LoadingState, ErrorState],
  templateUrl: './asset-edit-page.html',
  styleUrl: './asset-edit-page.css',
})
export class AssetEditPage {
  private readonly api = inject(AssetApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  @ViewChild(AssetForm) private form?: AssetForm;
  asset: Asset | null = null;
  formValue: AssetUpsertRequest | null = null;
  error: ApiError | null = null;
  loading = true;
  saving = false;
  private id = Number(this.route.snapshot.paramMap.get('id'));

  constructor() {
    this.load();
  }
  retry(): void {
    this.load();
  }
  cancel(): void {
    void this.router.navigate(['/assets', this.id]);
  }
  save(request: AssetUpsertRequest): void {
    if (this.saving) return;
    this.saving = true;
    this.error = null;
    this.api.updateAsset(this.id, request).subscribe({
      next: () => void this.router.navigate(['/assets', this.id]),
      error: (response) => {
        this.error = mapApiError(response);
        this.form?.applyServerErrors(this.error.errors);
        this.saving = false;
      },
    });
  }

  private load(): void {
    this.loading = true;
    this.error = null;
    this.api.getAsset(this.id).subscribe({
      next: (asset) => {
        this.asset = asset;
        this.formValue = {
          name: asset.name,
          assetCode: asset.assetCode,
          type: asset.type,
          location: asset.location,
          temperature: asset.temperature,
          pressure: asset.pressure,
        };
        this.loading = false;
      },
      error: (response) => {
        this.error = mapApiError(response);
        this.loading = false;
      },
    });
  }
}
