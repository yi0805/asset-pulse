import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AssetUpsertRequest } from '../../models/api.models';

@Component({
  selector: 'app-asset-form',
  imports: [ReactiveFormsModule],
  templateUrl: './asset-form.html',
  styleUrl: './asset-form.css',
})
export class AssetForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly initialValue = input<AssetUpsertRequest | null>(null);
  readonly saving = input(false);
  readonly submitted = output<AssetUpsertRequest>();
  readonly form = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    assetCode: ['', [Validators.required, Validators.maxLength(32)]],
    type: ['', [Validators.required, Validators.maxLength(50)]],
    location: ['', [Validators.required, Validators.maxLength(100)]],
    temperature: [null as number | null, [Validators.min(-273.15)]],
    pressure: [null as number | null, [Validators.min(0)]],
  });

  constructor() {
    effect(() => {
      const value = this.initialValue();
      if (value) {
        this.form.reset(value);
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      name: value.name ?? '',
      assetCode: value.assetCode ?? '',
      type: value.type ?? '',
      location: value.location ?? '',
      temperature: value.temperature,
      pressure: value.pressure,
    });
  }

  applyServerErrors(errors: Record<string, string[]> | undefined): void {
    for (const [key, messages] of Object.entries(errors ?? {})) {
      const matchingName = Object.keys(this.form.controls).find(
        (controlName) => controlName.toLowerCase() === key.toLowerCase(),
      );
      const control = matchingName
        ? this.form.controls[matchingName as keyof typeof this.form.controls]
        : undefined;
      if (control && messages.length > 0) {
        control.setErrors({ ...control.errors, server: messages[0] });
        control.markAsTouched();
      }
    }
  }

  errorFor(name: keyof typeof this.form.controls): string | null {
    const control = this.form.controls[name];
    if (!control.touched || !control.errors) {
      return null;
    }

    if (control.errors['server']) {
      return control.errors['server'] as string;
    }
    if (control.errors['required']) {
      return 'This field is required.';
    }
    if (control.errors['maxlength']) {
      return `Use no more than ${control.errors['maxlength'].requiredLength} characters.`;
    }
    if (control.errors['min']) {
      return `Use a value of at least ${control.errors['min'].min}.`;
    }
    return 'Enter a valid value.';
  }
}
