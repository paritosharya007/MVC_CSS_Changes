namespace Goalvisor.ViewModels.DataTables.Base
{
    public class Column
    {
        public bool SetSort(int order, int direction)
        {
            Sort.Column = order;
            Sort.Dir = direction == 0 ? SortDirection.Asc : SortDirection.Desc;
            return IsSortable;
        }

        public string Data { get; set; }
        public string Name { get; set; }
        public string Field { get; set; }
        public bool IsSearchable { get; set; }
        public Search Search { get; set; }
        public bool IsSortable { get; set; }
        public Sort Sort { get; set; }
    }

    public enum SortDirection
    {
        Asc = 0,
        Desc = 1
    }
}