import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReturnBook } from './return-book';

describe('ReturnBook', () => {
  let component: ReturnBook;
  let fixture: ComponentFixture<ReturnBook>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReturnBook],
    }).compileComponents();

    fixture = TestBed.createComponent(ReturnBook);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
