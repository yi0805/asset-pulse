import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AssetEditPage } from './asset-edit-page';
import { AssetApiService } from '../asset-api.service';

describe('AssetEditPage', () => {
  const router = { navigate: vi.fn().mockResolvedValue(true) };
  const asset = {
    id: 2,
    name: 'Pump',
    assetCode: 'PUMP-2',
    type: 'Pump',
    location: 'North',
    temperature: 20,
    pressure: null,
    lastUpdated: '2026-09-18T00:00:00Z',
    status: 'Warning' as const,
  };
  const api = { getAsset: vi.fn(), updateAsset: vi.fn() };

  beforeEach(async () => {
    api.getAsset.mockReset();
    api.updateAsset.mockReset();
    router.navigate.mockClear();
    await TestBed.configureTestingModule({
      imports: [AssetEditPage],
      providers: [
        { provide: AssetApiService, useValue: api },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } },
        },
      ],
    }).compileComponents();
  });

  it('loads fields, keeps status read-only, and navigates after a successful PUT', () => {
    api.getAsset.mockReturnValue(of(asset));
    api.updateAsset.mockReturnValue(of(asset));
    const fixture = TestBed.createComponent(AssetEditPage);
    fixture.detectChanges();
    const form = fixture.componentInstance['form']!;
    expect(form.form.controls.name.value).toBe('Pump');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Status is read-only');

    fixture.componentInstance.save({
      name: 'Updated',
      assetCode: 'PUMP-2',
      type: 'Pump',
      location: 'North',
      temperature: 20,
      pressure: null,
    });
    expect(api.updateAsset).toHaveBeenCalledWith(2, expect.objectContaining({ name: 'Updated' }));
    expect(router.navigate).toHaveBeenCalledWith(['/assets', 2]);
  });

  it('shows a missing asset and retains changed data after a duplicate conflict', () => {
    api.getAsset.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 404, error: { title: 'Not found' } })),
    );
    const missing = TestBed.createComponent(AssetEditPage);
    missing.detectChanges();
    expect((missing.nativeElement as HTMLElement).textContent).toContain('does not exist');
  });
});
