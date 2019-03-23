using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SaveChangesVSBulkSaveChanges
{
    class Program
    {
        public const string ConnectionString = @"ENTER CONNECTION STRING";

        private BloggingContext _context;

        static List<string> messages = new List<string>();

        static void Main(string[] args)
        {
            var now = DateTime.Now;
            Console.WriteLine("Application started.");

            var program = new Program();
            program.Run(10, true).Wait();
            program.Run(100, true).Wait();
            program.Run(1000, true).Wait();
            program.Run(10000, true).Wait();
            program.Run(100000, false).Wait();
            program.Run(1000000, false).Wait();

            Console.WriteLine($"Application ended in {(DateTime.Now - now)}.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var message in messages)
                Console.WriteLine(message);
            Console.ResetColor();

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        private async Task Run(int iterations, bool includeRelated)
        {
            this._context = new BloggingContext();
            await this._context.Database.MigrateAsync();
            double average = 0;
            double averageBulk = 0;
            double averageRelated = 0;
            double averageRelatedBulk = 0;

            for (int i = 0; i < 5; i++)
                average += await this.SaveChanges(this.GenerateData, iterations);
            for (int i = 0; i < 5; i++)
                averageBulk += await this.BulkSaveChanges(this.GenerateData, iterations);
            if (includeRelated)
            {
                for (int i = 0; i < 5; i++)
                    averageRelated += await this.SaveChanges(this.GenerateDataRelated, iterations);
                for (int i = 0; i < 5; i++)
                    averageRelatedBulk += await this.BulkSaveChanges(this.GenerateDataRelated, iterations);
            }

            var averageS = Math.Round(average / 5, 1).ToString("F1");
            var averageBulkS = Math.Round(averageBulk / 5, 1).ToString("F1");
            var averageRelatedS = Math.Round(averageRelated / 5, 1).ToString("F1");
            var averageRelatedBulkS = Math.Round(averageRelatedBulk / 5, 1).ToString("F1");
            var factorS = Math.Round(average / averageBulk, 1).ToString("F1");
            var factorRelatedS = Math.Round(averageRelated / averageRelatedBulk, 1).ToString("F1");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"          Average for SaveChanges from 5 tries is {averageS} seconds.");
            Console.WriteLine($"          Average for BulkSaveChanges from 5 tries is {averageS} seconds.");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Bulk is faster for {factorS} factor with {iterations} inserts.");
            if (includeRelated)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"          Average for related SaveChanges from 5 tries is {averageRelatedS} seconds.");
                Console.WriteLine($"          Average for related BulkSaveChanges from 5 tries is {averageRelatedBulkS} seconds.");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Bulk is faster in case of related data for {factorRelatedS} factor with {iterations} inserts.");
            }

            Console.ResetColor();

            messages.Add(string.Format("{0,8}{1,7}s{2,7}s{3,7}{4,7}{5,7}{6,7}", iterations, averageS, averageBulkS, factorS, includeRelated ? averageRelatedS + "s" : "/", includeRelated ? averageRelatedBulkS + "s" : "/", includeRelated ? factorRelatedS : "/"));
//            messages.Add(string.Format("{0,8}{1,5}{2,5}{3,5}{4,5}{5,5}{6,5}", iterations, average, averageBulk, factor, includeRelated ? averageRelated.ToString() : "/", includeRelated ? averageRelatedBulk.ToString() : "/", includeRelated ? factorRelated.ToString() : "/"));
        }

        private async Task<double> SaveChanges(Action<int> generateData, int iterations)
        {
            generateData(iterations);
            var now = DateTime.Now;
            await this._context.SaveChangesAsync();
            var result = (DateTime.Now - now).TotalSeconds;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Save changes completed in {result} seconds.");
            Console.ResetColor();
            return result;
        }

        private async Task<double> BulkSaveChanges(Action<int> generateData, int iterations)
        {
            generateData(iterations);
            var now = DateTime.Now;
            await this._context.BulkSaveChangesAsync();
            var result = (DateTime.Now - now).TotalSeconds;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Bulk save changes completed in {result} seconds.");
            Console.ResetColor();
            return result;
        }

        private void GenerateData(int iterations)
        {
            var now = DateTime.Now;
            var r = new Random();
            for (var i = 0; i < iterations; i++)
            {
                this._context.Blogs.Add(new Blog
                {
                    Rating = r.Next(1, 10),
                    Url = Extensions.GenerateRandomString(100)
                });
            }
        }

        private void GenerateDataRelated(int iterations)
        {
            var now = DateTime.Now;
            var r = new Random();
            for (var i = 0; i < iterations; i++)
            {
                var list = new List<Post>();
                for (var j = 0; j < 10; j++)
                    list.Add(new Post
                    {
                        Content = Extensions.GenerateRandomString(500),
                        Title = Extensions.GenerateRandomString(20),
                        Entered = DateTime.UtcNow
                    });
                this._context.Blogs.Add(new Blog
                {
                    Rating = r.Next(1, 10),
                    Url = Extensions.GenerateRandomString(100),
                    Posts = list
                });
            }
        }
    }

    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Program.ConnectionString);
        }
    }

    public class Blog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Url { get; set; }
        public int Rating { get; set; }
        public List<Post> Posts { get; set; }
    }

    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public DateTime Entered { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }

    public class Extensions
    {
        private static string UpperCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string LowerCharacters = "abcdefghijklmnoprstuvzxyqw";
        private static string NumberCharacters = "0123456789";
        private static string SpecialCharacters = ",.-_!#$%&/()=";

        /// <summary>
        /// Generates the random string.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="chars">The chars.</param>
        /// <returns>System.String.</returns>
        public static string GenerateRandomString(int length, string chars = null)
        {
            var random = new Random();

            return new string(Enumerable.Repeat(chars ?? UpperCharacters + LowerCharacters + NumberCharacters + SpecialCharacters, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }


}
