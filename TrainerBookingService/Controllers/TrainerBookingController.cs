using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public TrainerBookingController(
        ITrainerBookingRepository trainerBookingRepository,
        ILogger<TrainerBookingController> logger, 
        IHttpClientFactory httpClientFactory)
    {
        _trainerBookingRepository = trainerBookingRepository;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

    var userClient = _httpClientFactory.CreateClient("userService");
    userClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    var memberResponse = await userClient.GetAsync($"api/Member/{bookingDto.MemberId}");
    if (!memberResponse.IsSuccessStatusCode)
    {
        _logger.LogInformation("Member {MemberId} not found in UserService", bookingDto.MemberId);
        return NotFound($"Member {bookingDto.MemberId} not found");
    }

    /*var membershipClient = _httpClientFactory.CreateClient("membershipService");
    membershipClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    var membershipResponse = await membershipClient.GetAsync($"api/Membership/{bookingDto.MemberId}");
    if (!membershipResponse.IsSuccessStatusCode)
    {
        _logger.LogInformation("Member {MemberId} has no active membership", bookingDto.MemberId);
        return BadRequest($"Member {bookingDto.MemberId} has no active membership");
    }*/

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

    [Authorize]
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

    [Authorize(Roles = "Admin,Trainer")]    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBookings()
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetAllBookings));

        var bookings = await _trainerBookingRepository.GetAllBookings();
        return Ok(bookings);
    }
    
    [Authorize]
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
    
    [Authorize]
    [HttpGet]
    [Route("member/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBookingsByMember(int memberId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetBookingsByMember));

        if (memberId <= 0)
        {
            _logger.LogInformation("GetBookingsByMember called with invalid memberId");
            return BadRequest("Invalid memberId");
        }

        var bookings = await _trainerBookingRepository.GetBookingsByMember(memberId);
        return Ok(bookings);
    }
}