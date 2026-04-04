using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class AssignmentCategory
    {
        public AssignmentCategory()
        {
            Assignments = new HashSet<Assignment>();
        }

        public string CatName { get; set; } = null!;
        public uint CId { get; set; }
        public int AcId { get; set; }
        public uint GrdWeight { get; set; }

        public virtual Class CIdNavigation { get; set; } = null!;
        public virtual ICollection<Assignment> Assignments { get; set; }
    }
}
