using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
    {
        _logger.LogError("MemberId claim not found in token");
        return Unauthorized();
    }
    var memberId = int.Parse(userIdClaim.Value);
    
    if (bookingDto.TrainerId <= 0)
    {
        _logger.LogInformation("CreateBooking called with invalid trainerId");
        return BadRequest("Invalid trainerId");
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
    var memberResponse = await userClient.GetAsync($"api/Member/{memberId}");
    if (!memberResponse.IsSuccessStatusCode)
    {
        _logger.LogInformation("Member {MemberId} not found in UserService", memberId);
        return NotFound($"Member {memberId} not found");
    }

    /*var membershipClient = _httpClientFactory.CreateClient("membershipService");
    membershipClient.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    var membershipResponse = await membershipClient.GetAsync($"api/Membership/{memberId}");
    if (!membershipResponse.IsSuccessStatusCode)
    {
        _logger.LogInformation("Member {MemberId} has no active membership", memberId);
        return BadRequest($"Member {memberId} has no active membership");
    }*/

    var booking = await _trainerBookingRepository.CreateBooking(
        memberId,
        bookingDto.TrainerId,
        bookingDto.StartTime,
        bookingDto.EndTime);

    if (booking == null)
    {
        _logger.LogError("CreateBooking failed for memberId {MemberId} and trainerId {TrainerId}",
            memberId, bookingDto.TrainerId);
        return StatusCode(500, "Failed to create booking");
    }

    _logger.LogInformation("Booking created for memberId {MemberId} and trainerId {TrainerId}",
        memberId, bookingDto.TrainerId);
    return Created($"/api/TrainerBooking/{booking.BookingId}", booking);
}

    [Authorize]
    [HttpDelete]
    [Route("{bookingId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CancelBooking));

        if (bookingId <= 0)
        {
            _logger.LogInformation("CancelBooking called with invalid bookingId");
            return BadRequest("Invalid bookingId");
        }

        var booking = await _trainerBookingRepository.GetBooking(bookingId);
        if (booking == null)
        {
            _logger.LogInformation("Booking with id {BookingId} not found", bookingId);
            return NotFound($"Booking with id {bookingId} not found");
        }
        
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var isAdminOrTrainer = User.IsInRole("Admin") || User.IsInRole("Trainer");

        if (!isAdminOrTrainer && userIdClaim != null && booking.MemberId != int.Parse(userIdClaim.Value))
        {
            _logger.LogInformation("Member {MemberId} tried to cancel booking {BookingId} belonging to another member", userIdClaim.Value, bookingId);
            return Forbid();
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var isAdminOrTrainer = User.IsInRole("Admin") || User.IsInRole("Trainer");
        
        if (!isAdminOrTrainer && userIdClaim != null && booking.MemberId != int.Parse(userIdClaim.Value))
        {
            _logger.LogInformation("Booking with {BookingId} does not belong to member {MemberId}", bookingId, userIdClaim.Value);
            return Forbid();
        }
        
        return Ok(booking);
    }
    
    [Authorize]
    [HttpGet]
    [Route("member/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBookingsByMember(int memberId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetBookingsByMember));

        if (memberId <= 0)
        {
            _logger.LogInformation("GetBookingsByMember called with invalid memberId");
            return BadRequest("Invalid memberId");
        }

        var bookings = await _trainerBookingRepository.GetBookingsByMember(memberId);
        
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var isAdminOrTrainer = User.IsInRole("Admin") || User.IsInRole("Trainer");
        
        if (!isAdminOrTrainer && userIdClaim != null && memberId != int.Parse(userIdClaim.Value))
        {
            _logger.LogInformation("Member {MemberId} tried to access bookings for member {TargetMemberId}", userIdClaim.Value, memberId);
            return Forbid();
        }
        return Ok(bookings);
    }
}