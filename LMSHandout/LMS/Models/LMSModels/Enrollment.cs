using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Enrollment
    {
        public uint CId { get; set; }
        public string Student { get; set; } = null!;
        public string? Grade { get; set; }

        public virtual Class CIdNavigation { get; set; } = null!;
        public virtual Student StudentNavigation { get; set; } = null!;
    }
}
