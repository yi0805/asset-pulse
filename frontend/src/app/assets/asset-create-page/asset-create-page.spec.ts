import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetCreatePage } from './asset-create-page';
import { AssetApiService } from '../asset-api.service';

describe('AssetCreatePage', () => {
  const router = { navigate: vi.fn().mockResolvedValue(true) };
  const api = { createAsset: vi.fn() };

  beforeEach(async () => {
    api.createAsset.mockReset();
    router.navigate.mockClear();
    await TestBed.configureTestingModule({
      imports: [AssetCreatePage],
      providers: [
        { provide: AssetApiService, useValue: api },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
      ],
    }).compileComponents();
  });

  it('navigates to the created asset and prevents duplicate saves while saving', () => {
    api.createAsset.mockReturnValue(of({ id: 7 }));
    const component = TestBed.createComponent(AssetCreatePage).componentInstance;
    const request = {
      name: 'Pump',
      assetCode: 'PUMP-1',
      type: 'Pump',
      location: 'West',
      temperature: null,
      pressure: null,
    };

    component.save(request);
    component.save(request);

    expect(api.createAsset).toHaveBeenCalledTimes(1);
    expect(router.navigate).toHaveBeenCalledWith(['/assets', 7]);
  });

  it('preserves form data and maps duplicate conflicts to Asset code', () => {
    api.createAsset.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, title: 'Conflict', errors: { assetCode: ['Already exists'] } },
          }),
      ),
    );
    const fixture = TestBed.createComponent(AssetCreatePage);
    fixture.detectChanges();
    const form = fixture.componentInstance['form']!;
    form.form.patchValue({ name: 'Pump', assetCode: 'DUPLICATE' });

    fixture.componentInstance.save({
      name: 'Pump',
      assetCode: 'DUPLICATE',
      type: 'Pump',
      location: 'West',
      temperature: null,
      pressure: null,
    });

    expect(form.form.controls.assetCode.value).toBe('DUPLICATE');
    expect(form.errorFor('assetCode')).toContain('Already exists');
  });
});
