namespace Nanomite.Server.Authenticaton.Data.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Nanomite.Core.DataAccess.Database;
    using Nanomite.Core.Network.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Defines the <see cref="UserContext"/>
    /// </summary>
    public class UserContext : BaseContext
    {
        /// <summary>
        /// The database logger
        /// </summary>
        public static readonly ILoggerFactory DbLogger = new LoggerFactory().AddConsole();

        /// <summary>
        /// Gets or sets the database options.
        /// </summary>
        public new static DbContextOptions<UserContext> DatabaseOptions { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContext"/> class.
        /// </summary>
        public UserContext() : base(DatabaseOptions ?? new DbContextOptionsBuilder<UserContext>().UseSqlite($"Filename=UserContext.db3").Options) //.UseLoggerFactory(DbLogger)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContext"/> class.
        /// </summary>
        /// <param name="options">The <see cref="DbContextOptions{DatabaseContext}"/></param>
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        { }

        /// <inheritdoc />
        public override Version DatabaseVersion { get => new Version(1, 0, 3); }

        /// <inheritdoc />
        public override string DatabasePath { get => this.Database.GetDbConnection().DataSource; }

        /// <inheritdoc />
        public override IEnumerable<T> Include<T>(IQueryable<T> entities, string query = null, Func<T, bool> authQuery = null)
        {
            return BaseContext.Query(entities, authQuery, query);
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NetworkUser>(entity =>
            {
                entity.HasKey(p => p.Id);
            });
        }
    }
}
