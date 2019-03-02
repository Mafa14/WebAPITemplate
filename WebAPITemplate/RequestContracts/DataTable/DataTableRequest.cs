namespace WebAPITemplate.RequestContracts.DataTable
{
    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public Order[] Order { get; set; }
        public Column[] Columns { get; set; }
    }
}
