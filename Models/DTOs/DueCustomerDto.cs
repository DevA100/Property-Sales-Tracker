public class DueCustomerDto
{
    public int SaleId { get; set; }
    public string CustomerName { get; set; }
    public string PropertyName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool IsDiaspora { get; set; }
    public string OfficerName { get; set; }
    public string OfficerEmail { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysLeft { get; set; }
}
