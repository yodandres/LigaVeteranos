using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain
{
    public class Referee
    {
        [Key]
        public int RefereeId { get; set; }

        [Display(Name = "Referee Name")]
        public string Name { get; set; }

        [JsonIgnore]
        public virtual ICollection<Match> Matches { get; set; }
    }
}
