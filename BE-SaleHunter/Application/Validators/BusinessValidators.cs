using FluentValidation;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Store;

namespace BE_SaleHunter.Application.Validators
{
    public class CreateProductValidator : AbstractValidator<CreateProductDto>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.DiscountedPrice)
                .GreaterThan(0).WithMessage("Discounted price must be greater than 0")
                .LessThan(x => x.Price).WithMessage("Discounted price must be less than regular price")
                .When(x => x.DiscountedPrice.HasValue);

            RuleFor(x => x.Brand)
                .MaximumLength(100).WithMessage("Brand cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Brand));

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

            RuleFor(x => x.Images)
                .Must(x => x.Count <= 10).WithMessage("Maximum 10 images allowed")
                .When(x => x.Images != null);
        }
    }

    public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0")
                .When(x => x.Price.HasValue);

            RuleFor(x => x.DiscountedPrice)
                .GreaterThan(0).WithMessage("Discounted price must be greater than 0")
                .LessThan(x => x.Price).WithMessage("Discounted price must be less than regular price")
                .When(x => x.DiscountedPrice.HasValue && x.Price.HasValue);

            RuleFor(x => x.Brand)
                .MaximumLength(100).WithMessage("Brand cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Brand));

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));

            RuleFor(x => x.NewImages)
                .Must(x => x.Count <= 10).WithMessage("Maximum 10 new images allowed")
                .When(x => x.NewImages != null);
        }
    }

    public class CreateProductRatingValidator : AbstractValidator<CreateProductRatingDto>
    {
        public CreateProductRatingValidator()
        {
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Review cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Comment));
        }
    }

    public class CreateStoreValidator : AbstractValidator<CreateStoreDto>
    {
        public CreateStoreValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Store name is required")
                .MaximumLength(200).WithMessage("Store name cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required")
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required")
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
        }
    }

    public class UpdateStoreValidator : AbstractValidator<UpdateStoreDto>
    {
        public UpdateStoreValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("Store name cannot exceed 200 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Description));            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Address));

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Category)
                .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Category));
        }
    }

    public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }
}
