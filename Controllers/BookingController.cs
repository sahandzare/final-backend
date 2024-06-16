using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
namespace Camping.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BookingController : Controller
    {
        public readonly string _connectionString = "server=127.0.0.1;database=camping;uid=root;pwd=root";

        [HttpPost("book")]
        public IActionResult BookSpot([FromBody] booking bookingRequest)
        {
            try
            {
               
                if (!CampingSpotExists(bookingRequest.CampingSpotId))
                {
                    return NotFound($"Camping spot with ID {bookingRequest.CampingSpotId} not found.");
                }

                
                if (!IsSpotAvailable(bookingRequest.CampingSpotId, bookingRequest.CheckInDate, bookingRequest.CheckOutDate))
                {
                    return Conflict($"Camping spot with ID {bookingRequest.CampingSpotId} is already booked for the requested dates.");
                }

                
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO bookings (UserId, CampingSpotId, BookingDate, CheckIn, CheckOut) " +
                                            "VALUES (@UserId, @CampingSpotId, @BookingDate, @CheckIn, @CheckOut);" +
                                            "SELECT LAST_INSERT_ID();";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", bookingRequest.UserId);
                        command.Parameters.AddWithValue("@CampingSpotId", bookingRequest.CampingSpotId);
                        command.Parameters.AddWithValue("@BookingDate", DateTime.Now);
                        command.Parameters.AddWithValue("@CheckIn", bookingRequest.CheckInDate);
                        command.Parameters.AddWithValue("@CheckOut", bookingRequest.CheckOutDate);

                        int bookingId = Convert.ToInt32(command.ExecuteScalar());

                        if (bookingId > 0)
                        {
                            
                            UpdateSpotAvailability(connection, bookingRequest.CampingSpotId, -1);
                            return Ok(new { BookingId = bookingId }); // Return booking ID in JSON format
                        }
                        else
                        {
                            return StatusCode(500, "Failed to book the spot. Please try again later.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [HttpDelete("cancel/{bookingId}")]
        public IActionResult CancelBooking(int bookingId)
        {
            try
            {
                // Retrieve booking information
                booking booking = GetBookingById(bookingId);
                if (booking == null)
                {
                    return NotFound($"Booking with ID {bookingId} not found.");
                }

                // Cancel the booking
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string deleteQuery = "DELETE FROM bookings WHERE BookingId = @BookingId";
                    using (var command = new MySqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@BookingId", bookingId);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Update availability of the canceled spot
                            UpdateSpotAvailability(connection, booking.CampingSpotId, 1);
                            return Ok("Booking canceled successfully.");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to cancel booking. Please try again later.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool CampingSpotExists(int campingSpotId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM spots WHERE Id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", campingSpotId);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private bool IsSpotAvailable(int campingSpotId, DateTime checkInDate, DateTime checkOutDate)
        {
           
            if (checkInDate >= checkOutDate)
            {
                return false; 
            }

            
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM bookings " +
                               "WHERE CampingSpotId = @CampingSpotId " +
                               "AND (@CheckIn < CheckOut AND @CheckOut > CheckIn)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CampingSpotId", campingSpotId);
                    command.Parameters.AddWithValue("@CheckIn", checkInDate);
                    command.Parameters.AddWithValue("@CheckOut", checkOutDate);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count == 0;
                }
            }
        }


        private void UpdateSpotAvailability(MySqlConnection connection, int campingSpotId, int change)
        {
            string updateQuery = "UPDATE spots SET Availability = Availability + @Change WHERE Id = @CampingSpotId";
            using (var command = new MySqlCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@Change", change);
                command.Parameters.AddWithValue("@CampingSpotId", campingSpotId);
                command.ExecuteNonQuery();
            }
            
        }

        private booking GetBookingById(int bookingId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM bookings WHERE BookingId = @BookingId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BookingId", bookingId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new booking
                            {

                                UserId = reader.GetInt32("UserId"),
                                CampingSpotId = reader.GetInt32("CampingSpotId"),
                                CheckInDate = reader.GetDateTime("CheckIn"),
                                CheckOutDate = reader.GetDateTime("CheckOut")

                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        [HttpGet("BookingsOverview")]

        public IActionResult GetUserBookings([FromHeader] string username, [FromHeader] string password)
        {
            try
            {
                // Validate user credentials
                bool isAuthenticated = AuthenticateUser(username, password);
                if (!isAuthenticated)
                {
                    return Unauthorized("Invalid username or password.");
                }

                // Get user ID based on username
                int userId = GetUserIdByUsername(username);

                // Fetch the bookings associated with the authenticated user from the database
                List<booking> userBookings = GetUserBookingsFromDatabase(userId);

                
                return Ok(userBookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            // Query the database to validate the user's credentials
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM users WHERE Username = @Username AND Password = @Password";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private int GetUserIdByUsername(string username)
        {
           
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Id FROM users WHERE Username = @Username";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        throw new Exception("User not found.");
                    }
                }
            }
        }

        private List<booking> GetUserBookingsFromDatabase(int userId)
        {
            List<booking> userBookings = new List<booking>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT BookingId, UserId, CampingSpotId, BookingDate, CheckIn, CheckOut " +
                               "FROM bookings " +
                               "WHERE UserId = @UserId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var booking = new booking
                            {
                                BookingId = reader.GetInt32("BookingId"),
                                UserId = reader.GetInt32("UserId"),
                                CampingSpotId = reader.GetInt32("CampingSpotId"),
                                BookingDate = reader.GetDateTime("BookingDate"),
                                CheckInDate = reader.GetDateTime("CheckIn"),
                                CheckOutDate = reader.GetDateTime("CheckOut")
                            };
                            userBookings.Add(booking);
                        }
                    }
                }
            }

            return userBookings;
        }
    }


}
