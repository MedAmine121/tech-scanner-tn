using FluentValidation;
using Hi_Trade.Models.Requests;

namespace Hi_Trade.Models.Validators;

public class LoginUserValidator : AbstractValidator<LoginUserRequest>
{
    public LoginUserValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

