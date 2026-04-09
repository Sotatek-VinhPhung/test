using CleanArchitecture.Application.Users.DTOs;
using FluentValidation;

namespace CleanArchitecture.Application.Users.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
