namespace Goalvisor.ViewModels.DataTables.Base
{
    public class DataTableResponse
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public object Data { get; set; }
        //public string Error { get; set; }
        //public IDictionary<string, object> AdditionalParameters { get; set; }

        public static DataTableResponse Create(int count, int i, object dataPage)
        {
            return new DataTableResponse
            {
                Data = dataPage,
                RecordsTotal = count,
                RecordsFiltered = i
            };
        }
    }
}