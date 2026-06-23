import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadPlan } from './upload-plan';

describe('UploadPlan', () => {
  let component: UploadPlan;
  let fixture: ComponentFixture<UploadPlan>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UploadPlan],
    }).compileComponents();

    fixture = TestBed.createComponent(UploadPlan);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
