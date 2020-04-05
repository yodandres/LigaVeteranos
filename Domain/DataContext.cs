using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Domain
{
    public class DataContext : DbContext
    {
        public DataContext() : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            modelBuilder.Configurations.Add(new UsersMap());
            modelBuilder.Configurations.Add(new MatchesMap());
        }

        public System.Data.Entity.DbSet<Domain.League> Leagues { get; set; }

        public System.Data.Entity.DbSet<Domain.Team> Teams { get; set; }

        public System.Data.Entity.DbSet<Domain.Tournament> Tournaments { get; set; }

        public System.Data.Entity.DbSet<Domain.Date> Dates { get; set; }

        public System.Data.Entity.DbSet<Domain.Match> Matches { get; set; }

        public System.Data.Entity.DbSet<Domain.Prediction> Predictions { get; set; }

        public System.Data.Entity.DbSet<Domain.TournamentGroup> TournamentGroups { get; set; }

        public System.Data.Entity.DbSet<Domain.User> Users { get; set; }

        public System.Data.Entity.DbSet<Domain.UserType> UserTypes { get; set; }

        public System.Data.Entity.DbSet<Domain.TournamentTeam> TournamentTeams { get; set; }
    }
}
