namespace BE_SaleHunter.Application.DTOs;

public class PagingRequestDto
{
    private int _size = 10;
    private int _page;
    
    public int Size
    {
        get => _size;
        init => _size = value > 0 ? value : 10; // Default to 10 if size is less than or equal to 0
    }
    
    public int Page
    {
        get => _page;
        init => _page = value >= 0 ? value : 0; // Default to 0 if page is less than 0
    }
}