using BE_SaleHunter.Application.DTOs.Order;
namespace BE_SaleHunter.Application.DTOs.Payment
{
    public record CreatePaymentLinkRequest
    (
        TempOrderSession order,
        string description,
        int price,
        string returnUrl,
        string cancelUrl
    );
}
