namespace BE_SaleHunter.Application.DTOs.Store
{
    public class DashboardStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; } // Will be 0 for now
        public int TotalCustomers { get; set; } // Will be 0 for now
        public int TotalViews { get; set; }
    }

    public class MonthlySalesDto
    {
        public string Month { get; set; } = string.Empty;
        public int Sales { get; set; } // Representing count of products for now
    }

    public class CategoryDistributionDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class SellerDashboardDto
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<MonthlySalesDto> SalesByMonth { get; set; } = new();
        public List<CategoryDistributionDto> CategoryDistribution { get; set; } = new();
    }
}
