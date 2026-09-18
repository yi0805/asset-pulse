import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

interface PlaceholderData {
  title: string;
  description: string;
}

@Component({
  selector: 'app-placeholder-page',
  templateUrl: './placeholder-page.html',
  styleUrl: './placeholder-page.css',
})
export class PlaceholderPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly data = this.route.snapshot.data as PlaceholderData;
}
