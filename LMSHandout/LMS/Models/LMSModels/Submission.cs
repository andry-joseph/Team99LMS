using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Submission
    {
        public uint AId { get; set; }
        public string Student { get; set; } = null!;
        public DateTime Time { get; set; }
        public string StudentSolution { get; set; } = null!;
        public uint? Score { get; set; }

        public virtual Assignment AIdNavigation { get; set; } = null!;
        public virtual Student StudentNavigation { get; set; } = null!;
    }
}
