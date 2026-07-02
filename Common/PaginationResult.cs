using MyApiBlya.Services; 
public class PaginationReult
{
    public List<User>? users{get;set;}
    public int Page {get; set;}
    public int PageSize{get; set;}
    public int TotalCount{get;set;}

}