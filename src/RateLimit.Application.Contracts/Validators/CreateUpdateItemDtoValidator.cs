using FluentValidation;
using RateLimit.Consts;
using RateLimit.Interfaces.Dtos;

namespace RateLimit.Validators;
public class CreateUpdateItemDtoValidator : AbstractValidator<CreateUpdateItemDto>
{
    public CreateUpdateItemDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(ItemConsts.MaxLength.Name).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");
    }
}