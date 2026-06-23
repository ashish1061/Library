import { ComponentFixture, TestBed } from '@angular/core/testing';

import { IssueBook } from './issue-book';

describe('IssueBook', () => {
  let component: IssueBook;
  let fixture: ComponentFixture<IssueBook>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [IssueBook],
    }).compileComponents();

    fixture = TestBed.createComponent(IssueBook);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
