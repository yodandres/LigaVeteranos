using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Backend.Models
{
    public class TeamList
    {
        public int TeamId { get; set; }

        public string Name { get; set; }
        
        public string Logo { get; set; }
        
        public string Initials { get; set; }
        
        public int LeagueId { get; set; }
    }
}