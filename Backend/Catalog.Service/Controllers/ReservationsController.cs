using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Domain;
using Shared.Infrastructure.Repositories;

namespace Catalog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationRepository _reservationRepository;

        public ReservationsController(IReservationRepository reservationRepository)
        {
            _reservationRepository = reservationRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] Reservation reservation)
        {
            reservation.ReservationDate = DateTime.Now;
            reservation.Status = "Pending";
            var id = await _reservationRepository.AddReservationAsync(reservation);
            return Ok(new { Id = id });
        }

        [HttpGet("book/{anum}")]
        public async Task<IActionResult> GetForBook(long anum)
        {
            return Ok(await _reservationRepository.GetReservationsForBookAsync(anum));
        }

        [HttpGet("user/{empId}")]
        public async Task<IActionResult> GetForUser(string empId)
        {
            return Ok(await _reservationRepository.GetReservationsByUserAsync(empId));
        }
    }
}
