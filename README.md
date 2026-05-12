# TrainerBookingService

A microservice for managing personal trainer bookings. The service allows gym members of FitLife to book a personal trainer for a specific time slot, cancel existing bookings, and retrieve booking information to see their booking history.

## Endpoints

- `POST /api/TrainerBooking` - Create a new trainer booking
- `DELETE /api/TrainerBooking/{bookingId}` - Cancel a booking
- `GET /api/TrainerBooking` - Get all bookings
- `GET /api/TrainerBooking/{bookingId}` - Get a specific booking
- `GET /api/TrainerBooking/member/{memberId}` - Get all bookings for a specific member

## Technologies

- C#
- ASP.NET Core
- MongoDB
- NLog
