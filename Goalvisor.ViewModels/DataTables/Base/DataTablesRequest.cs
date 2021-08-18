using System.Collections.Generic;

namespace Goalvisor.ViewModels.DataTables.Base
{
    public class DataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public Search Search { get; set; } = new Search();
        public List<Order> Order { get; set; } = new List<Order>();
        public IEnumerable<Column> Columns { get; set; }
        public IDictionary<string, object> AdditionalParameters { get; set; }
    }

    public class Sort
    {
        public SortDirection Dir { get; set; }
        public int Column { get; set; }

        public void SetSortDirection(string direction)
        {
            if (direction.Equals("asc"))
            {
                this.Dir = SortDirection.Asc;
            }
            else
            {
                this.Dir = SortDirection.Desc;
            }
        }
    }
}