using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace API.Models
{
    public class Result
    {
        public int PredictionId { get; set; }

        public int UserId { get; set; }
        
        public int MatchId { get; set; }
        
        public int LocalGoals { get; set; }
        
        public int VisitorGoals { get; set; }

        public int Points { get; set; }
        
        public MatchResponse Match { get; set; }
    }
}