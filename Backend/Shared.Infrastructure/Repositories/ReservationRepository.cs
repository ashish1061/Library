using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Shared.Core.Domain;

namespace Shared.Infrastructure.Repositories
{
    public interface IReservationRepository
    {
        Task<int> AddReservationAsync(Reservation reservation);
        Task<IEnumerable<Reservation>> GetReservationsForBookAsync(long anum);
        Task<IEnumerable<Reservation>> GetReservationsByUserAsync(string empId);
    }

    public class ReservationRepository : IReservationRepository
    {
        private readonly string _connectionString;

        public ReservationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LibraryDB") ?? throw new ArgumentNullException("Connection string not found!");
        }

        public async Task<int> AddReservationAsync(Reservation reservation)
        {
            using var db = new SqlConnection(_connectionString);
            var query = @"
                INSERT INTO Reservations (Anum, EmpID, ReservationDate, Status)
                VALUES (@Anum, @EmpID, @ReservationDate, @Status);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";
            return await db.ExecuteScalarAsync<int>(query, reservation);
        }

        public async Task<IEnumerable<Reservation>> GetReservationsForBookAsync(long anum)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<Reservation>("SELECT * FROM Reservations WHERE Anum = @Anum", new { Anum = anum });
        }

        public async Task<IEnumerable<Reservation>> GetReservationsByUserAsync(string empId)
        {
            using var db = new SqlConnection(_connectionString);
            return await db.QueryAsync<Reservation>("SELECT * FROM Reservations WHERE EmpID = @EmpID", new { EmpID = empId });
        }
    }
}
