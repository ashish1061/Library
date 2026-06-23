using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;

namespace Shared.Infrastructure.Repositories
{
    public interface IBookRepository
    {
        Task<IEnumerable<Book>> GetAllBooksAsync();
        Task<Book?> GetBookByIdAsync(long anum);
        Task<int> AddBookAsync(Book book);
        Task<IEnumerable<Book>> SearchBooksAsync(string category, string keyword);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }

    public class BookRepository : IBookRepository
    {
        private readonly string _connectionString;

        public BookRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<Book>("SELECT * FROM Books");
        }

        public async Task<Book?> GetBookByIdAsync(long anum)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QuerySingleOrDefaultAsync<Book>("SELECT * FROM Books WHERE Anum = @Anum", new { Anum = anum });
        }

        public async Task<int> AddBookAsync(Book book)
        {
            using var db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Anum", book.Anum);
            parameters.Add("@Book_name", book.Book_name);
            parameters.Add("@Book_author", book.Book_author);
            parameters.Add("@Book_rack", book.Book_rack);
            parameters.Add("@Book_class", book.Book_class);
            parameters.Add("@Book_category", book.Book_category);
            parameters.Add("@Available", book.Available);

            parameters.Add("@Publisher", book.Publisher);
            parameters.Add("@ISBN", book.ISBN);
            parameters.Add("@Edition", book.Edition);
            parameters.Add("@TotalCopies", book.TotalCopies);
            parameters.Add("@CoverImagePath", book.CoverImagePath);

            var query = @"
                IF EXISTS (SELECT 1 FROM Books WHERE Anum = @Anum)
                BEGIN
                    UPDATE Books 
                    SET Book_name = @Book_name, Book_author = @Book_author, Book_rack = @Book_rack, 
                        Book_class = @Book_class, Book_category = @Book_category, Available = CASE WHEN @TotalCopies = 0 THEN 0 ELSE @Available END,
                        Publisher = @Publisher, ISBN = @ISBN, Edition = @Edition, TotalCopies = @TotalCopies, 
                        CoverImagePath = CASE WHEN ISNULL(@CoverImagePath, '') = '' THEN CoverImagePath ELSE @CoverImagePath END
                    WHERE Anum = @Anum;
                END
                ELSE
                BEGIN
                    INSERT INTO Books (Anum, Book_name, Book_author, Book_rack, Book_class, Book_category, Available, 
                                       Publisher, ISBN, Edition, TotalCopies, CoverImagePath)
                    VALUES (@Anum, @Book_name, @Book_author, @Book_rack, @Book_class, @Book_category, CASE WHEN @TotalCopies = 0 THEN 0 ELSE @Available END, 
                            @Publisher, @ISBN, @Edition, @TotalCopies, @CoverImagePath);
                END
            ";

            return await db.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string category, string keyword)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                SELECT 
                    b.Anum, b.Book_name, b.Book_author, b.Book_rack, b.Book_class, b.Book_category, b.Available, b.Publisher, b.ISBN, b.Edition, b.TotalCopies, b.CoverImagePath,
                    i.EmpName as IssuedTo
                FROM Books b
                LEFT JOIN Issue i ON b.Anum = i.Anum
                WHERE (@Category IS NULL OR @Category = '' OR b.Book_category = @Category)
                  AND (@Keyword IS NULL OR @Keyword = '' OR b.Book_name LIKE '%' + @Keyword + '%' OR b.Book_author LIKE '%' + @Keyword + '%' OR b.ISBN LIKE '%' + @Keyword + '%')
            ";
            
            var parameters = new DynamicParameters();
            parameters.Add("@Category", category);
            parameters.Add("@Keyword", keyword);
            
            return await db.QueryAsync<Book>(query, parameters);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            using var db = new SqlConnection(_connectionString);
            var query = "SELECT DISTINCT Book_category FROM Books WHERE Book_category IS NOT NULL AND Book_category <> '' ORDER BY Book_category";
            return await db.QueryAsync<string>(query);
        }
    }
}
