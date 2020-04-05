using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Field
    {
        [Key]
        public int FieldId { get; set; }

        [Display(Name = "Field Name")]
        public string Name { get; set; }

        [JsonIgnore]
        public virtual ICollection<Match> Matches { get; set; }
    }
}
