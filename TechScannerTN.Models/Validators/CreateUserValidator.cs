using FluentValidation;
using Hi_Trade.Models.Requests;

namespace Hi_Trade.Models.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(user => user.FullName)
            .NotEmpty().WithMessage("Full Name is required.")
            .MinimumLength(2).WithMessage("Full Name must be at least 2 characters long.");

        RuleFor(user => user.Address)
            .NotEmpty().WithMessage("Address is required.");
    }
}

