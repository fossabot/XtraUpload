import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ComponentBase } from 'app/shared';
import { takeUntil, finalize } from 'rxjs/operators';
import { ISignupParams } from 'app/domain';
import { AuthService, SeoService } from 'app/services';

@Component({
  selector: 'app-signup',
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.css']
})
export class SignupComponent extends ComponentBase implements OnInit {
  private readonly pageTitle = 'Signup';
  signupFormGroup: FormGroup;
  userName = new FormControl('', [Validators.required, Validators.minLength(4)]);
  email = new FormControl('', [Validators.required, Validators.email]);
  password = new FormControl('', [Validators.required, Validators.minLength(6)]);
  termsOfService = new FormControl(false, [Validators.requiredTrue]);
  hidePassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private seoService: SeoService) {
    super();
    seoService.setPageTitle(this.pageTitle);
  }
  ngOnInit(): void {
    this.signupFormGroup = this.fb.group({
      email: this.email,
      userName: this.userName,
      password: this.password,
      termsOfService : this.termsOfService
    });
  }
  getEmailErrorMessage() {
    return this.email.hasError('required') ? 'You must enter a value' :
        this.email.hasError('email') ? 'Not a valid email' : '';
  }
  onSubmit(signupParams: ISignupParams) {
    this.isBusy = true;
    this.authService.requestSignup(signupParams)
    .pipe(
      takeUntil(this.onDestroy),
      finalize(() => this.isBusy = false))
    .subscribe(
      () => {
        this.message$.next({successMessage: `Account successfully created, please log in.`});
        this.resetForm(this.signupFormGroup);
      },
      error => {
        this.message$.next({errorMessage: error?.error?.errorContent?.message});
        throw error;
      }
    );
  }
}
