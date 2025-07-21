using Azure;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Application.DTOs.Order;
using BE_SaleHunter.Application.DTOs.Payment;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Net.payOS;
using Net.payOS.Types;
using System.Security.Claims;

namespace BE_SaleHunter.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly PayOS _payOS;
        private readonly IOrderService _orderService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<ProductController> _logger;
        public OrderController(IOrderService orderService, ILogger<ProductController> logger, PayOS payOS, IMemoryCache memoryCache)
        {
            _orderService = orderService;
            _memoryCache = memoryCache;
            _payOS = payOS;
            _logger = logger;
        }
        /// <summary>
        /// tạo đơn hàng mới
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePaymentLink(CreatePaymentLinkRequest body)
        {
            try
            {
                int orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
                List<ItemData> items = new List<ItemData>();
                int totalPrice = 0;
                foreach (var CartItemDTO in body.order.Items)
                {
                    ItemData item = new ItemData(CartItemDTO.ProductName, CartItemDTO.Quantity, CartItemDTO.Quantity * (int)CartItemDTO.Price);
                    totalPrice += CartItemDTO.Quantity * (int)CartItemDTO.Price;
                    items.Add(item);
                }

                PaymentData paymentData = new PaymentData(orderCode, totalPrice, body.description, items, body.cancelUrl, body.returnUrl);

                CreatePaymentResult createPayment = await _payOS.createPaymentLink(paymentData);
                string cacheKey = $"{orderCode}";
                _memoryCache.Set(cacheKey, body.order, TimeSpan.FromMinutes(30));
                KeyTracker.OrderKeys.Add(cacheKey);
                return Ok(BaseResponseDto<CreatePaymentResult>.Success(createPayment, "success"));
            }
            catch (System.Exception exception)
            {
                Console.WriteLine(exception);
                return Ok(BaseResponseDto<CreatePaymentResult>.Success(null, "fail"));
            }
        }
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder([FromRoute] int orderId)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.getPaymentLinkInformation(orderId);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success(paymentLinkInformation, "success"));
            }
            catch (System.Exception exception)
            {

                Console.WriteLine(exception);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success("fail"));
            }

        }
        [HttpPut("{orderId}")]
        public async Task<IActionResult> CancelOrder([FromRoute] int orderId)
        {
            try
            {
                PaymentLinkInformation paymentLinkInformation = await _payOS.cancelPaymentLink(orderId);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success(paymentLinkInformation, "success")); ;
            }
            catch (System.Exception exception)
            {

                Console.WriteLine(exception);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success(null, "fail"));
            }

        }
        [HttpPost("confirm-webhook")]
        public async Task<IActionResult> ConfirmWebhook(ConfirmWebhook  body)
        {
            try
            {
                await _payOS.confirmWebhook(body.webhook_url);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success(null, "OK"));
            }
            catch (System.Exception exception)
            {

                Console.WriteLine(exception);
                return Ok(BaseResponseDto<PaymentLinkInformation>.Success(null, "fail"));
            }

        }
        /// <summary>
        /// Update order status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateOrderDto"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseDto<OrderDto>>> UpdateOrder(long id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<ProductDto>.Failure("Invalid user"));
                }

                var result = await _orderService.UpdateOrderAsync(id, updateOrderDto, userId.Value);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return StatusCode(500, BaseResponseDto<ProductDto>.Failure("Internal server error"));
            }
        }
        /// <summary>
        /// get all order of store
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrderOfAStore(long storeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return BadRequest(BaseResponseDto<ProductDto>.Failure("Invalid user"));
                }

                var result = await _orderService.GetOrderStore(storeId);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponseDto<ProductDto>.Failure("Internal server error"));
            }
        }
        /// <summary>
        /// Get order detail
        /// </summary>
        [HttpGet("success/{orderId}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(long orderId)
        {
            try
            {
                //var userId = GetCurrentUserId();
                //if (userId == null)
                //{
                //    return BadRequest(BaseResponseDto<OrderDto>.Failure("Invalid user"));
                //}

                var result = await _orderService.GetOrderById(orderId);

                if (result.IsSuccess)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponseDto<OrderDto>.Failure("Internal server error"));
            }
        }
        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
