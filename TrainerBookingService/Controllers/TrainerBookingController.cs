using Microsoft.AspNetCore.Mvc;
using Models;
using TrainerBookingService.Repositories.Interfaces;

namespace TrainerBookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrainerBookingController : ControllerBase
{
    private readonly ITrainerBookingRepository _trainerBookingRepository;
    private readonly ILogger<TrainerBookingController> _logger;

    public TrainerBookingController(
        ITrainerBookingRepository trainerBookingRepository,
        ILogger<TrainerBookingController> logger)
    {
        _trainerBookingRepository = trainerBookingRepository;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBooking([FromBody] TrainerBookingDto bookingDto)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CreateBooking));

        if (bookingDto.MemberId <= 0 || bookingDto.TrainerId <= 0)
        {
            _logger.LogInformation("CreateBooking called with invalid memberId or trainerId");
            return BadRequest("Invalid memberId or trainerId");
        }

        if (bookingDto.StartTime >= bookingDto.EndTime)
        {
            _logger.LogInformation("CreateBooking called with invalid time range");
            return BadRequest("StartTime must be before EndTime");
        }

        var booking = await _trainerBookingRepository.CreateBooking(
            bookingDto.MemberId,
            bookingDto.TrainerId,
            bookingDto.StartTime,
            bookingDto.EndTime);
            
        if (booking == null)
        {
            _logger.LogError("CreateBooking failed for memberId {MemberId} and trainerId {TrainerId}",
                bookingDto.MemberId, bookingDto.TrainerId);
            return StatusCode(500, "Failed to create booking");
        }

        _logger.LogInformation("Booking created for memberId {MemberId} and trainerId {TrainerId}",
            bookingDto.MemberId, bookingDto.TrainerId);
        return Created($"/api/TrainerBooking/{booking.BookingId}", booking);
    }

    [HttpDelete]
    [Route("{bookingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CancelBooking));

        if (bookingId <= 0)
        {
            _logger.LogInformation("CancelBooking called with invalid bookingId");
            return BadRequest("Invalid bookingId");
        }

        var result = await _trainerBookingRepository.CancelBooking(bookingId);
        if (!result)
        {
            _logger.LogError("CancelBooking failed for bookingId {BookingId}", bookingId);
            return NotFound($"Booking with id {bookingId} not found");
        }

        _logger.LogInformation("Booking with id {BookingId} cancelled", bookingId);
        return Ok();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBookings()
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetAllBookings));

        var bookings = await _trainerBookingRepository.GetAllBookings();
        return Ok(bookings);
    }

    [HttpGet]
    [Route("{bookingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(int bookingId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetBooking));

        if (bookingId <= 0)
        {
            _logger.LogInformation("GetBooking called with invalid bookingId");
            return BadRequest("Invalid bookingId");
        }

        var booking = await _trainerBookingRepository.GetBooking(bookingId);
        if (booking == null)
        {
            _logger.LogInformation("Booking with id {BookingId} not found", bookingId);
            return NotFound();
        }

        return Ok(booking);
    }
}