namespace EasyCreditFrontend.Models;

// Envelope standar Easycredit API : {status, message, data:}
public class ApiResponse<T>
{
    public bool Status {get; set;}
    public string Message {get; set;} = string.Empty;
    public T? Data {get; set;}
    public bool IsSuccess => Status;
}

// Bentuk data untuk GET /api/customers: {page, size, total, items: [{id, name, email, phone, createdAt}, ...]}
public class PageData<T>
{
    public int Page {get; set;}
    public int Size {get; set;}
    public int PageSize => Size;
    public int Total {get; set;}
    public List<T> Items {get; set;} = new();
}

// bentu data untuk get /api/credit-applications : {total, items}
public class TotalItemsData<T>
{
    public int Total {get; set;}
    public List<T> Items {get; set;} = new();
}