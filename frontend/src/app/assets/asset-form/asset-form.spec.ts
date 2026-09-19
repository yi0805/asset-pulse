import { TestBed } from '@angular/core/testing';
import { AssetForm } from './asset-form';

describe('AssetForm', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<AssetForm>>;
  let component: AssetForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AssetForm] }).compileComponents();
    fixture = TestBed.createComponent(AssetForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('validates required values, lengths, and measurement minima', () => {
    component.form.patchValue({
      name: '',
      assetCode: 'A'.repeat(33),
      type: '',
      location: '',
      temperature: -273.16,
      pressure: -0.01,
    });
    component.form.markAllAsTouched();

    expect(component.errorFor('name')).toContain('required');
    expect(component.errorFor('assetCode')).toContain('32');
    expect(component.errorFor('temperature')).toContain('-273.15');
    expect(component.errorFor('pressure')).toContain('0');
  });

  it('submits only editable fields and maps blank measurements to null', () => {
    let submitted: unknown;
    component.submitted.subscribe((request) => (submitted = request));
    component.form.setValue({
      name: 'Pump',
      assetCode: 'pump-01',
      type: 'Pump',
      location: 'West',
      temperature: null,
      pressure: null,
    });

    component.submit();

    expect(submitted).toEqual({
      name: 'Pump',
      assetCode: 'pump-01',
      type: 'Pump',
      location: 'West',
      temperature: null,
      pressure: null,
    });
    expect(fixture.nativeElement.textContent).not.toContain('Status');
  });

  it('applies a server duplicate-code error to the matching control case-insensitively', () => {
    component.applyServerErrors({ ASSETCODE: ['An asset with this asset code already exists.'] });

    expect(component.errorFor('assetCode')).toContain('already exists');
  });
});
