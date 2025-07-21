using AutoMapper;
using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;

namespace BE_SaleHunter.Application.Services
{
    public interface IOrderService
    {
        Task<BaseResponseDto<OrderDto>> UpdateOrderAsync(long orderId, UpdateOrderDto model, long id);
        Task<BaseResponseDto<IEnumerable<OrderDto>>> GetOrderStore(long id);
        Task<BaseResponseDto<OrderDto>> GetOrderById(long orderId);
    }
    public class OrderService(IUnitOfWork unitOfWork,
        IMapper mapper) : IOrderService
    {
        public async Task<BaseResponseDto<OrderDto>> UpdateOrderAsync(long orderId, UpdateOrderDto model, long id)
        {
            try
            {
                var order = await unitOfWork.GenericRepository<Order>().FirstOrDefaultAsync(c => c.Id == orderId);
                if (order == null)
                {
                    return BaseResponseDto<OrderDto>.Failure("Order not found");
                }
                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    order.Status = model.Status;
                }
                order.UpdatedAt = DateTime.UtcNow;
                await unitOfWork.GenericRepository<Order>().UpdateAsync(order);
                await unitOfWork.CompleteAsync();
                var orderDto = mapper.Map<OrderDto>(order);
                return BaseResponseDto<OrderDto>.Success(orderDto, "order updated successfully");
            }
            catch (Exception ex)
            {
                return BaseResponseDto<OrderDto>.Failure(ex.Message);
            }
        }
        public async Task<BaseResponseDto<IEnumerable<OrderDto>>> GetOrderStore(long storeId)
        {
            try
            {
                var orders = await unitOfWork.OrderRepository.GetOrderOfStore(storeId);

                if (orders == null)
                {
                    return BaseResponseDto<IEnumerable<OrderDto>>.Failure("Order not found");
                }
                var orderDto = mapper.Map<IEnumerable<OrderDto>>(orders);
                return BaseResponseDto<IEnumerable<OrderDto>>.Success(orderDto, "order updated successfully");
            }
            catch (Exception ex)
            {
                return BaseResponseDto<IEnumerable<OrderDto>>.Failure(ex.Message);
            }
        }

        public async Task<BaseResponseDto<OrderDto>> GetOrderById(long orderId)
        {
            try
            {
                var orders = await unitOfWork.OrderRepository.GetOrderById(orderId);

                if (orders == null)
                {
                    return BaseResponseDto<OrderDto>.Failure("Order not found");
                }
                var orderDto = mapper.Map<OrderDto>(orders);
                return BaseResponseDto<OrderDto>.Success(orderDto, "order updated successfully");
            }
            catch (Exception ex)
            {
                return BaseResponseDto<OrderDto>.Failure(ex.Message);
            }
        }
    }
}
